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
// <auto-generated />
namespace HealthGateway.Patient.Test
{
    using Xunit;
    using Microsoft.Extensions.Logging;
    using Moq;
    using System.Threading.Tasks;
    using HealthGateway.Common.Models;
    using HealthGateway.Common.Delegates;
    using HealthGateway.Common.Services;
    using Microsoft.Extensions.Configuration;
    using HealthGateway.Database.Delegates;
    using System.Collections.Generic;
    using HealthGateway.Common.Constants;

    public class PatientService_Test
    {
        //this.config = new ConfigurationBuilder()
                // .AddJsonFile("UnitTest.json").Build();

        [Fact]
        public void ShouldGetPatient()
        {
            string hdid = "abc123";

            RequestResult<PatientModel> requestResult = new RequestResult<PatientModel>()
            {
                ResultStatus = Common.Constants.ResultType.Success,
                TotalResultCount = 1,
                PageSize = 1,
                ResourcePayload = new PatientModel()
                {
                    FirstName = "John",
                    LastName = "Doe",
                    HdId = hdid,
                },
            };

            Mock<IClientRegistriesDelegate> patientDelegateMock = new Mock<IClientRegistriesDelegate>();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>())
                .Build();

            patientDelegateMock.Setup(p => p.GetDemographicsByHDIDAsync(It.IsAny<string>())).ReturnsAsync(requestResult);

            IPatientService service = new PatientService(
                new Mock<ILogger<PatientService>>().Object,
                configuration,
                patientDelegateMock.Object,
                new Mock<IGenericCacheDelegate>().Object);

            // Act
            RequestResult<PatientModel> actual = Task.Run(async () => await service.GetPatient(hdid).ConfigureAwait(true)).Result;

            // Verify
            Assert.Equal(Common.Constants.ResultType.Success, actual.ResultStatus);
            Assert.Equal(hdid, actual.ResourcePayload.HdId);
        }

        [Fact]
        public void ShoulSearchByValidIdentifier()
        {
            string phn = "abc123";

            RequestResult<PatientModel> requestResult = new RequestResult<PatientModel>()
            {
                ResultStatus = Common.Constants.ResultType.Success,
                TotalResultCount = 1,
                PageSize = 1,
                ResourcePayload = new PatientModel()
                {
                    FirstName = "John",
                    LastName = "Doe",
                    PersonalHealthNumber = phn,
                },
            };

            Mock<IClientRegistriesDelegate> patientDelegateMock = new Mock<IClientRegistriesDelegate>();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>())
                .Build();
            patientDelegateMock.Setup(p => p.GetDemographicsByPHNAsync(It.IsAny<string>())).ReturnsAsync(requestResult);

            IPatientService service = new PatientService(
                new Mock<ILogger<PatientService>>().Object,
                configuration,
                patientDelegateMock.Object,
                new Mock<IGenericCacheDelegate>().Object);

            // Act
            RequestResult<PatientModel> actual = Task.Run(async () => await service.GetPatient("abc123",Common.Constants.PatientIdentifierType.PHN).ConfigureAwait(true)).Result;            

            // Verify
            Assert.Equal(Common.Constants.ResultType.Success, actual.ResultStatus);
            Assert.Equal(phn, actual.ResourcePayload.PersonalHealthNumber);
        }

        [Fact]
        public void ShoulBeEmptyIfInvalidIdentifier()
        {
            string phn = "abc123";

            RequestResult<PatientModel> requestResult = new RequestResult<PatientModel>()
            {
                ResultStatus = Common.Constants.ResultType.Success,
                TotalResultCount = 1,
                PageSize = 1,
                ResourcePayload = new PatientModel()
                {
                    FirstName = "John",
                    LastName = "Doe",
                    PersonalHealthNumber = phn,
                },
            };

            Mock<IClientRegistriesDelegate> patientDelegateMock = new Mock<IClientRegistriesDelegate>();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>())
                .Build();
            patientDelegateMock.Setup(p => p.GetDemographicsByHDIDAsync(It.IsAny<string>())).ReturnsAsync(requestResult);

            IPatientService service = new PatientService(
                new Mock<ILogger<PatientService>>().Object,
                configuration,
                patientDelegateMock.Object,
                new Mock<IGenericCacheDelegate>().Object);

            // Act
            RequestResult<PatientModel> actual = Task.Run(async () => await service.GetPatient("abc123", (PatientIdentifierType)23).ConfigureAwait(true)).Result;

            // Verify
            Assert.Equal(Common.Constants.ResultType.Error, actual.ResultStatus);
        }
    }
}
