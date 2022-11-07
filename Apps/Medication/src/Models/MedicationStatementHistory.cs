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
namespace HealthGateway.Medication.Models
{
    using System;

    /// <summary>
    /// The medications statement data model.
    /// </summary>
    public class MedicationStatementHistory
    {
        /// <summary>
        /// Gets or sets the brand name of the  medication.
        /// </summary>
        public string PrescriptionIdentifier { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Prescription status.
        /// </summary>
        public char PrescriptionStatus { get; set; }

        /// <summary>
        /// Gets or sets the date the medication was dispensed.
        /// </summary>
        public DateTime DispensedDate { get; set; }

        /// <summary>
        /// Gets or sets the Surname of the Practitioner who prescribed the medication.
        /// </summary>
        public string PractitionerSurname { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the directions as prescribed.
        /// </summary>
        public string Directions { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the date the medication was entered.
        /// </summary>
        public DateTime? DateEntered { get; set; }

        /// <summary>
        /// Gets or sets the pharmacy id.
        /// </summary>
        public string PharmacyId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the medication for the current MedicationStatementHistory.
        /// </summary>
        public MedicationSummary MedicationSummary { get; set; } = new();

        /// <summary>
        /// Gets or sets the dispensing pharmacy for the current MedicationStatementHistory.
        /// </summary>
        public Pharmacy DispensingPharmacy { get; set; } = new();
    }
}
