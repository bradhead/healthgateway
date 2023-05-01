//-------------------------------------------------------------------------
// Copyright © 2019 Province of British Columbia
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//-------------------------------------------------------------------------
namespace HealthGateway.Admin.Client.Pages
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Threading.Tasks;
    using Fluxor;
    using Fluxor.Blazor.Web.Components;
    using HealthGateway.Admin.Client.Store.PatientSupport;
    using HealthGateway.Admin.Common.Constants;
    using HealthGateway.Admin.Common.Models;
    using HealthGateway.Common.Data.Constants;
    using HealthGateway.Common.Data.Utils;
    using HealthGateway.Common.Data.Validations;
    using Microsoft.AspNetCore.Components;
    using Microsoft.AspNetCore.WebUtilities;
    using Microsoft.Extensions.Primitives;
    using MudBlazor;

    /// <summary>
    /// Backing logic for the Support page.
    /// </summary>
    public partial class SupportPage : FluxorComponent
    {
        private static readonly PhnValidator PhnValidator = new();

        private static List<PatientQueryType> QueryTypes => new() { PatientQueryType.Phn, PatientQueryType.Email, PatientQueryType.Sms, PatientQueryType.Hdid, PatientQueryType.Dependent };

        [Inject]
        private IDispatcher Dispatcher { get; set; } = default!;

        [Inject]
        private IState<PatientSupportState> PatientSupportState { get; set; } = default!;

        [Inject]
        private NavigationManager NavigationManager { get; set; } = default!;

        private PatientQueryType QueryType { get; set; } = PatientQueryType.Phn;

        private PatientQueryType SelectedQueryType
        {
            get => this.QueryType;

            set
            {
                this.ResetPatientSupportState();
                this.QueryParameter = string.Empty;
                this.QueryType = value;
            }
        }

        private string QueryParameter { get; set; } = string.Empty;

        private MudForm Form { get; set; } = default!;

        private bool PhnOrDependentSelected => this.SelectedQueryType is PatientQueryType.Phn or PatientQueryType.Dependent;

        private bool PatientsLoading => this.PatientSupportState.Value.IsLoading;

        private bool PatientsLoaded => this.PatientSupportState.Value.Loaded;

        private bool HasPatientsError => this.PatientSupportState.Value.Error is { Message.Length: > 0 };

        private bool HasPatientsWarning => this.PatientSupportState.Value.WarningMessages.Any();

        private IImmutableList<PatientSupportDetails> Patients => this.PatientSupportState.Value.Result ?? ImmutableList<PatientSupportDetails>.Empty;

        private IEnumerable<PatientRow> PatientRows => this.Patients.Select(v => new PatientRow(v));

        private Func<string, string?> ValidateQueryParameter => parameter =>
        {
            if (string.IsNullOrWhiteSpace(parameter))
            {
                return "Search parameter is required";
            }

            if (this.PhnOrDependentSelected && !PhnValidator.Validate(StringManipulator.StripWhitespace(parameter)).IsValid)
            {
                return "Invalid PHN";
            }

            return this.SelectedQueryType switch
            {
                PatientQueryType.Email or PatientQueryType.Sms when StringManipulator.StripWhitespace(parameter)?.Length < 5
                    => "Email/SMS must be minimum 5 characters",
                PatientQueryType.Sms when !StringManipulator.IsPositiveNumeric(parameter)
                    => "SMS must contain digits only",
                _ => null,
            };
        };

        /// <inheritdoc/>
        protected override void OnInitialized()
        {
            base.OnInitialized();
            this.ResetPatientSupportState();

            Uri uri = this.NavigationManager.ToAbsoluteUri(this.NavigationManager.Uri);

            if (QueryHelpers.ParseQuery(uri.Query).TryGetValue(PatientQueryType.Hdid.ToString(), out StringValues hdid))
            {
                this.Dispatcher.Dispatch(new PatientSupportActions.LoadAction(PatientQueryType.Hdid, StringManipulator.StripWhitespace(hdid)));
                this.QueryParameter = hdid!;
                this.SelectedQueryType = PatientQueryType.Hdid;
            }
        }

        private static string FormatQueryType(PatientQueryType queryType)
        {
            return queryType switch
            {
                PatientQueryType.Hdid => "HDID",
                PatientQueryType.Phn => "PHN",
                PatientQueryType.Sms => "SMS",
                _ => queryType.ToString(),
            };
        }

        private void ResetPatientSupportState()
        {
            this.Dispatcher.Dispatch(new PatientSupportActions.ResetStateAction());
        }

        private async Task SearchAsync()
        {
            await this.Form.Validate().ConfigureAwait(true);
            if (this.Form.IsValid)
            {
                this.ResetPatientSupportState();
                this.Dispatcher.Dispatch(new PatientSupportActions.LoadAction(this.SelectedQueryType, StringManipulator.StripWhitespace(this.QueryParameter)));
            }
        }

        private sealed record PatientRow
        {
            public PatientRow(PatientSupportDetails model)
            {
                this.Status = model.Status;
                this.Name = StringManipulator.JoinWithoutBlanks(new[] { model.PreferredName?.GivenName, model.PreferredName?.Surname });
                this.SortName = $"{model.PreferredName?.Surname}, {model.PreferredName?.GivenName}";
                this.Birthdate = model.Birthdate;
                this.PersonalHealthNumber = model.PersonalHealthNumber;
                this.Hdid = model.Hdid;
            }

            public PatientStatus Status { get; }

            public string Name { get; }

            public string SortName { get; }

            public DateOnly? Birthdate { get; }

            public string PersonalHealthNumber { get; }

            public string Hdid { get; }
        }
    }
}
