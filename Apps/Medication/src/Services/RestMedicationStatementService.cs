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
namespace HealthGateway.Medication.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Net.Mime;
    using System.Threading.Tasks;
    using HealthGateway.Common.Authentication;
    using HealthGateway.Common.Authentication.Models;
    using HealthGateway.Database.Constant;
    using HealthGateway.Database.Delegates;
    using HealthGateway.Database.Models;
    using HealthGateway.Medication.Models;
    using HealthGateway.Medication.Parsers;
    using Microsoft.Extensions.Configuration;
    using Newtonsoft.Json;

    /// <summary>
    /// The Medication data service.
    /// </summary>
    public class RestMedicationStatementService : IMedicationStatementService
    {
        private readonly IHttpClientFactory httpClientFactory;
        private readonly IConfiguration configService;
        private readonly IHNMessageParser<List<MedicationStatement>> medicationParser;
        private readonly IAuthService authService;
        private readonly ISequenceDelegate sequenceDelegate;
        private readonly IPharmacyService pharmacyService;
        private readonly IDrugLookupDelegate drugLookupDelegate;

        /// <summary>
        /// Initializes a new instance of the <see cref="RestMedicationStatementService"/> class.
        /// </summary>
        /// <param name="parser">The injected hn parser.</param>
        /// <param name="httpClientFactory">The injected http client factory.</param>
        /// <param name="configuration">The injected configuration provider.</param>
        /// <param name="authService">The injected authService for client credentials grant (system account).</param>
        /// <param name="sequenceDelegate">The injected sequence delegate.</param>
        /// <param name="pharmacyService">The injected pharmacy lookup service.</param>
        /// <param name="drugLookupDelegate">The injected drug lookup delegate.</param>
        public RestMedicationStatementService(
            IHNMessageParser<List<MedicationStatement>> parser,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            IAuthService authService,
            ISequenceDelegate sequenceDelegate,
            IPharmacyService pharmacyService,
            IDrugLookupDelegate drugLookupDelegate)
        {
            this.medicationParser = parser;
            this.httpClientFactory = httpClientFactory;
            this.configService = configuration;
            this.authService = authService;
            this.sequenceDelegate = sequenceDelegate;
            this.pharmacyService = pharmacyService;
            this.drugLookupDelegate = drugLookupDelegate;
        }

        /// <inheritdoc/>
        public async Task<HNMessage<List<MedicationStatement>>> GetMedicationStatementsAsync(string phn, string userId, string ipAddress)
        {
            JWTModel jwtModel = this.authService.AuthenticateService();
            HNMessage<List<MedicationStatement>> hnClientMedicationResult;
            using (HttpClient client = this.httpClientFactory.CreateClient("medicationService"))
            {
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Json));
                client.BaseAddress = new Uri(this.configService.GetSection("HNClient")?.GetValue<string>("Url"));
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + jwtModel.AccessToken);

                long traceId = this.sequenceDelegate.NextValueForSequence(Sequence.PHARMANET_TRACE);
                HNMessage<string> requestMessage = this.medicationParser.CreateRequestMessage(phn, userId, ipAddress, traceId);
                HttpResponseMessage response = await client.PostAsJsonAsync("v1/api/HNClient", requestMessage).ConfigureAwait(true);
                if (response.IsSuccessStatusCode)
                {
                    string payload = await response.Content.ReadAsStringAsync().ConfigureAwait(true);
                    HNMessage<string> responseMessage = JsonConvert.DeserializeObject<HNMessage<string>>(payload);
                    hnClientMedicationResult = this.medicationParser.ParseResponseMessage(responseMessage.Message);
                }
                else
                {
                    return new HNMessage<List<MedicationStatement>>(true, $"Unable to connect to HNClient: {response.StatusCode}");
                }
            }

            if (!hnClientMedicationResult.IsError)
            {
                this.PopulateBrandName(hnClientMedicationResult.Message);
            }

            return hnClientMedicationResult;
        }

        private void PopulateBrandName(List<MedicationStatement> statements)
        {
            List<string> medicationIdentifiers = statements.Select(s => s.MedicationSumary.DIN.PadLeft(8, '0')).ToList();

            Dictionary<string, string> brandNameMap = this.drugLookupDelegate.GetDrugsBrandNameByDIN(medicationIdentifiers);

            foreach (MedicationStatement medicationStatement in statements)
            {
                string din = medicationStatement.MedicationSumary.DIN.PadLeft(8, '0');
                if (brandNameMap.ContainsKey(din))
                {
                    medicationStatement.MedicationSumary.BrandName = brandNameMap[din];
                }
                else
                {
                    medicationStatement.MedicationSumary.BrandName = "Unknown brand name";
                }
            }
        }
    }
}