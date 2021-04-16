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
namespace HealthGateway.Medication.Services.Test
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Net;
    using System.Security.Claims;
    using System.Security.Principal;
    using System.Threading.Tasks;
    using HealthGateway.Common.Constants;
    using HealthGateway.Common.ErrorHandling;
    using HealthGateway.Common.Models;
    using HealthGateway.Common.Models.ODR;
    using HealthGateway.Common.Services;
    using HealthGateway.Database.Delegates;
    using HealthGateway.Database.Models;
    using HealthGateway.Medication.Delegates;
    using HealthGateway.Medication.Models;
    using HealthGateway.Medication.Models.ODR;
    using HealthGateway.Medication.Services;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using Moq;
    using Xunit;

    public class MedicationStatementService_Test
    {
        private readonly IConfiguration configuration;

        public MedicationStatementService_Test()
        {
            this.configuration = new ConfigurationBuilder().AddJsonFile("UnitTest.json").Build();
        }

        [Fact]
        public async Task InvalidProtectiveWord()
        {
            // Setup
            string hdid = "EXTRIOYFPNX35TWEBUAJ3DNFDFXSYTBC6J4M76GYE3HC5ER2NKWQ";
            string phn = "0009735353315";
            string userId = "1001";
            string ipAddress = "10.0.0.1";

            Mock<IIdentity> identityMock = new Mock<IIdentity>();
            identityMock.Setup(s => s.Name).Returns(userId);

            Mock<ClaimsPrincipal> claimsPrincipalMock = new Mock<ClaimsPrincipal>();
            claimsPrincipalMock.Setup(s => s.Identity).Returns(identityMock.Object);

            Mock<ConnectionInfo> connectionInfoMock = new Mock<ConnectionInfo>();
            connectionInfoMock.Setup(s => s.RemoteIpAddress).Returns(IPAddress.Parse(ipAddress));

            IHeaderDictionary headerDictionary = new HeaderDictionary();
            headerDictionary.Add("Authorization", "Bearer TestJWT");
            Mock<HttpRequest> httpRequestMock = new Mock<HttpRequest>();
            httpRequestMock.Setup(s => s.Headers).Returns(headerDictionary);

            Mock<HttpContext> httpContextMock = new Mock<HttpContext>();
            httpContextMock.Setup(s => s.Connection).Returns(connectionInfoMock.Object);
            httpContextMock.Setup(s => s.User).Returns(claimsPrincipalMock.Object);
            httpContextMock.Setup(s => s.Request).Returns(httpRequestMock.Object);

            Mock<IHttpContextAccessor> httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            httpContextAccessorMock.Setup(s => s.HttpContext).Returns(httpContextMock.Object);

            Mock<IPatientService> patientDelegateMock = new Mock<IPatientService>();
            patientDelegateMock.Setup(s => s.GetPatientPHN(hdid)).Returns(Task.FromResult(
                new RequestResult<string>()
                {
                    ResourcePayload = phn,
                    ResultStatus = Common.Constants.ResultType.Success,
                }));

            Mock<IDrugLookupDelegate> drugLookupDelegateMock = new Mock<IDrugLookupDelegate>();
            drugLookupDelegateMock.Setup(p => p.GetDrugProductsByDIN(It.IsAny<List<string>>())).Returns(new List<DrugProduct>());

            Mock<IMedStatementDelegate> medStatementDelegateMock = new Mock<IMedStatementDelegate>();
            RequestResult<MedicationHistoryResponse> requestResult = new RequestResult<MedicationHistoryResponse>();
            requestResult.ResourcePayload = new MedicationHistoryResponse();
            medStatementDelegateMock.Setup(p => p.GetMedicationStatementsAsync(It.IsAny<OdrHistoryQuery>(), null, It.IsAny<string>(), ipAddress)).ReturnsAsync(requestResult);

            IMedicationStatementService service = new RestMedicationStatementService(
                new Mock<ILogger<RestMedicationStatementService>>().Object,
                httpContextAccessorMock.Object,
                patientDelegateMock.Object,
                drugLookupDelegateMock.Object,
                medStatementDelegateMock.Object);

            // Run and Verify protective word too long
            RequestResult<IList<MedicationStatementHistory>> actual = await service.GetMedicationStatementsHistory(hdid, "TOOLONG4U").ConfigureAwait(true);
            Assert.Equal(ResultType.ActionRequired, actual.ResultStatus);
            Assert.Equal(ActionType.Protected, actual?.ResultError?.ActionCode);
            Assert.Equal(ErrorMessages.ProtectiveWordTooLong, actual?.ResultError?.ResultMessage);

            // Run and Verify invalid char
            actual = await service.GetMedicationStatementsHistory(hdid, "SHORT").ConfigureAwait(true);
            Assert.Equal(ResultType.ActionRequired, actual.ResultStatus);
            Assert.Equal(ActionType.Protected, actual?.ResultError?.ActionCode);
            Assert.Equal(ErrorMessages.ProtectiveWordTooShort, actual?.ResultError?.ResultMessage);

            // Run and Verify invalid char
            actual = await service.GetMedicationStatementsHistory(hdid, "SHORT|").ConfigureAwait(true);
            Assert.Equal(ResultType.ActionRequired, actual.ResultStatus);
            Assert.Equal(ActionType.Protected, actual?.ResultError?.ActionCode);
            Assert.Equal(ErrorMessages.ProtectiveWordInvalidChars, actual?.ResultError?.ResultMessage);

            // Run and Verify invalid char
            actual = await service.GetMedicationStatementsHistory(hdid, "SHORT~").ConfigureAwait(true);
            Assert.Equal(ResultType.ActionRequired, actual.ResultStatus);
            Assert.Equal(ActionType.Protected, actual?.ResultError?.ActionCode);
            Assert.Equal(ErrorMessages.ProtectiveWordInvalidChars, actual?.ResultError?.ResultMessage);

            // Run and Verify invalid char
            actual = await service.GetMedicationStatementsHistory(hdid, "SHORT^").ConfigureAwait(true);
            Assert.Equal(ResultType.ActionRequired, actual.ResultStatus);
            Assert.Equal(ActionType.Protected, actual?.ResultError?.ActionCode);
            Assert.Equal(ErrorMessages.ProtectiveWordInvalidChars, actual?.ResultError?.ResultMessage);

            // Run and Verify invalid char
            actual = await service.GetMedicationStatementsHistory(hdid, "SHORT\\").ConfigureAwait(true);
            Assert.Equal(ResultType.ActionRequired, actual.ResultStatus);
            Assert.Equal(ActionType.Protected, actual?.ResultError?.ActionCode);
            Assert.Equal(ErrorMessages.ProtectiveWordInvalidChars, actual?.ResultError?.ResultMessage);

            // Run and Verify invalid char
            actual = await service.GetMedicationStatementsHistory(hdid, "SHORT&").ConfigureAwait(true);
            Assert.Equal(ResultType.ActionRequired, actual.ResultStatus);
            Assert.Equal(ActionType.Protected, actual?.ResultError?.ActionCode);
            Assert.Equal(ErrorMessages.ProtectiveWordInvalidChars, actual?.ResultError?.ResultMessage);

            // Run and Verify invalid char
            actual = await service.GetMedicationStatementsHistory(hdid, "      ").ConfigureAwait(true);
            Assert.Equal(ResultType.ActionRequired, actual.ResultStatus);
            Assert.Equal(ActionType.Protected, actual?.ResultError?.ActionCode);
            Assert.Equal(ErrorMessages.ProtectiveWordInvalidChars, actual?.ResultError?.ResultMessage);
        }

        [Fact]
        public async Task ValidProtectiveWord()
        {
            // Setup
            string hdid = "EXTRIOYFPNX35TWEBUAJ3DNFDFXSYTBC6J4M76GYE3HC5ER2NKWQ";
            string protectiveWord = "TestWord";
            string phn = "0009735353315";
            string userId = "1001";
            string ipAddress = "10.0.0.1";

            Mock<IIdentity> identityMock = new Mock<IIdentity>();
            identityMock.Setup(s => s.Name).Returns(userId);

            Mock<ClaimsPrincipal> claimsPrincipalMock = new Mock<ClaimsPrincipal>();
            claimsPrincipalMock.Setup(s => s.Identity).Returns(identityMock.Object);

            Mock<ConnectionInfo> connectionInfoMock = new Mock<ConnectionInfo>();
            connectionInfoMock.Setup(s => s.RemoteIpAddress).Returns(IPAddress.Parse(ipAddress));

            IHeaderDictionary headerDictionary = new HeaderDictionary();
            headerDictionary.Add("Authorization", "Bearer TestJWT");
            Mock<HttpRequest> httpRequestMock = new Mock<HttpRequest>();
            httpRequestMock.Setup(s => s.Headers).Returns(headerDictionary);

            Mock<HttpContext> httpContextMock = new Mock<HttpContext>();
            httpContextMock.Setup(s => s.Connection).Returns(connectionInfoMock.Object);
            httpContextMock.Setup(s => s.User).Returns(claimsPrincipalMock.Object);
            httpContextMock.Setup(s => s.Request).Returns(httpRequestMock.Object);

            Mock<IHttpContextAccessor> httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            httpContextAccessorMock.Setup(s => s.HttpContext).Returns(httpContextMock.Object);

            Mock<IPatientService> patientDelegateMock = new Mock<IPatientService>();
            patientDelegateMock.Setup(s => s.GetPatient(hdid, Common.Constants.PatientIdentifierType.HDID)).Returns(Task.FromResult(
                new RequestResult<PatientModel>()
                {
                    ResourcePayload = new PatientModel()
                    {
                        Birthdate = DateTime.Parse("2000/01/31", CultureInfo.CurrentCulture),
                        FirstName = "Patient",
                        LastName = "Zero",
                        EmailAddress = "test@email.com",
                        HdId = hdid,
                        PersonalHealthNumber = phn,
                    },
                    ResultStatus = Common.Constants.ResultType.Success,
                }));

            Mock<IDrugLookupDelegate> drugLookupDelegateMock = new Mock<IDrugLookupDelegate>();
            drugLookupDelegateMock.Setup(p => p.GetDrugProductsByDIN(It.IsAny<List<string>>())).Returns(new List<DrugProduct>());

            Mock<IMedStatementDelegate> medStatementDelegateMock = new Mock<IMedStatementDelegate>();
            RequestResult<MedicationHistoryResponse> requestResult = new RequestResult<MedicationHistoryResponse>()
            {
                ResultStatus = Common.Constants.ResultType.Success,
            };
            requestResult.ResourcePayload = new MedicationHistoryResponse();
            medStatementDelegateMock.Setup(p => p.GetMedicationStatementsAsync(It.IsAny<OdrHistoryQuery>(), It.IsAny<string>(), It.IsAny<string>(), ipAddress)).ReturnsAsync(requestResult);

            IMedicationStatementService service = new RestMedicationStatementService(
                new Mock<ILogger<RestMedicationStatementService>>().Object,
                httpContextAccessorMock.Object,
                patientDelegateMock.Object,
                drugLookupDelegateMock.Object,
                medStatementDelegateMock.Object);

            // Run and Verify
            RequestResult<IList<MedicationStatementHistory>> actual = await service.GetMedicationStatementsHistory(hdid, protectiveWord).ConfigureAwait(true);
            Assert.True(actual.ResultStatus == Common.Constants.ResultType.Success);
        }

        [Fact]
        public void ShouldGetMedications()
        {
            // Setup
            string hdid = "EXTRIOYFPNX35TWEBUAJ3DNFDFXSYTBC6J4M76GYE3HC5ER2NKWQ";
            string phn = "0009735353315";
            string userId = "1001";
            string ipAddress = "10.0.0.1";
            string din = "00000000";

            Mock<IIdentity> identityMock = new Mock<IIdentity>();
            identityMock.Setup(s => s.Name).Returns(userId);

            Mock<ClaimsPrincipal> claimsPrincipalMock = new Mock<ClaimsPrincipal>();
            claimsPrincipalMock.Setup(s => s.Identity).Returns(identityMock.Object);

            Mock<ConnectionInfo> connectionInfoMock = new Mock<ConnectionInfo>();
            connectionInfoMock.Setup(s => s.RemoteIpAddress).Returns(IPAddress.Parse(ipAddress));

            IHeaderDictionary headerDictionary = new HeaderDictionary();
            headerDictionary.Add("Authorization", "Bearer TestJWT");
            Mock<HttpRequest> httpRequestMock = new Mock<HttpRequest>();
            httpRequestMock.Setup(s => s.Headers).Returns(headerDictionary);

            Mock<HttpContext> httpContextMock = new Mock<HttpContext>();
            httpContextMock.Setup(s => s.Connection).Returns(connectionInfoMock.Object);
            httpContextMock.Setup(s => s.User).Returns(claimsPrincipalMock.Object);
            httpContextMock.Setup(s => s.Request).Returns(httpRequestMock.Object);

            Mock<IHttpContextAccessor> httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            httpContextAccessorMock.Setup(s => s.HttpContext).Returns(httpContextMock.Object);

            Mock<IPatientService> patientDelegateMock = new Mock<IPatientService>();
            patientDelegateMock.Setup(s => s.GetPatient(hdid, Common.Constants.PatientIdentifierType.HDID)).Returns(Task.FromResult(
                new RequestResult<PatientModel>()
                {
                    ResourcePayload = new PatientModel()
                    {
                        Birthdate = DateTime.Parse("2000/01/31", CultureInfo.CurrentCulture),
                        FirstName = "Patient",
                        LastName = "Zero",
                        EmailAddress = "test@email.com",
                        HdId = hdid,
                        PersonalHealthNumber = phn,
                    },
                    ResultStatus = Common.Constants.ResultType.Success,
                }));

            Mock<IDrugLookupDelegate> drugLookupDelegateMock = new Mock<IDrugLookupDelegate>();

            // We need two tests, one for Fed data and one for Provincial data
            List<DrugProduct> drugList = new List<DrugProduct>()
            {
                new DrugProduct()
                {
                    DrugIdentificationNumber = din,
                    BrandName = "Brand Name",
                    Form = new Form()
                    {
                        PharmaceuticalForm = "PharmaceuticalForm",
                    },
                    ActiveIngredient = new ActiveIngredient()
                    {
                        Strength = "strength",
                        StrengthUnit = "strengthunit",
                    },
                    Company = new Company()
                    {
                        CompanyName = "Company",
                    },
                },
            };

            drugLookupDelegateMock.Setup(p => p.GetDrugProductsByDIN(It.IsAny<List<string>>())).Returns(drugList);

            Mock<IMedStatementDelegate> medStatementDelegateMock = new Mock<IMedStatementDelegate>();
            RequestResult<MedicationHistoryResponse> requestResult = new RequestResult<MedicationHistoryResponse>()
            {
                ResultStatus = Common.Constants.ResultType.Success,
                ResourcePayload = new MedicationHistoryResponse()
                {
                    TotalRecords = 1,
                    Pages = 1,
                    Results = new List<Models.ODR.MedicationResult>()
                    {
                        new Models.ODR.MedicationResult()
                        {
                            DIN = din,
                            GenericName = "Generic Name",
                        },
                    },
                },
            };
            medStatementDelegateMock.Setup(p => p.GetMedicationStatementsAsync(It.IsAny<OdrHistoryQuery>(), null, It.IsAny<string>(), ipAddress)).ReturnsAsync(requestResult);

            IMedicationStatementService service = new RestMedicationStatementService(
                new Mock<ILogger<RestMedicationStatementService>>().Object,
                httpContextAccessorMock.Object,
                patientDelegateMock.Object,
                drugLookupDelegateMock.Object,
                medStatementDelegateMock.Object);

            // Act
            RequestResult<IList<MedicationStatementHistory>> actual = Task.Run(async () => await service.GetMedicationStatementsHistory(hdid, null).ConfigureAwait(true)).Result;

            // Verify
            Assert.True(actual.ResultStatus == ResultType.Success && actual?.ResourcePayload?.Count == 1);
        }

        [Fact]
        public void ShouldGetMedicationsDrugInfoMissing()
        {
            // Setup
            string hdid = "EXTRIOYFPNX35TWEBUAJ3DNFDFXSYTBC6J4M76GYE3HC5ER2NKWQ";
            string phn = "0009735353315";
            string userId = "1001";
            string ipAddress = "10.0.0.1";
            string din = "00000000";

            Mock<IIdentity> identityMock = new Mock<IIdentity>();
            identityMock.Setup(s => s.Name).Returns(userId);

            Mock<ClaimsPrincipal> claimsPrincipalMock = new Mock<ClaimsPrincipal>();
            claimsPrincipalMock.Setup(s => s.Identity).Returns(identityMock.Object);

            Mock<ConnectionInfo> connectionInfoMock = new Mock<ConnectionInfo>();
            connectionInfoMock.Setup(s => s.RemoteIpAddress).Returns(IPAddress.Parse(ipAddress));

            IHeaderDictionary headerDictionary = new HeaderDictionary();
            headerDictionary.Add("Authorization", "Bearer TestJWT");
            Mock<HttpRequest> httpRequestMock = new Mock<HttpRequest>();
            httpRequestMock.Setup(s => s.Headers).Returns(headerDictionary);

            Mock<HttpContext> httpContextMock = new Mock<HttpContext>();
            httpContextMock.Setup(s => s.Connection).Returns(connectionInfoMock.Object);
            httpContextMock.Setup(s => s.User).Returns(claimsPrincipalMock.Object);
            httpContextMock.Setup(s => s.Request).Returns(httpRequestMock.Object);

            Mock<IHttpContextAccessor> httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            httpContextAccessorMock.Setup(s => s.HttpContext).Returns(httpContextMock.Object);

            Mock<IPatientService> patientDelegateMock = new Mock<IPatientService>();
            patientDelegateMock.Setup(s => s.GetPatient(hdid, Common.Constants.PatientIdentifierType.HDID)).Returns(Task.FromResult(
                new RequestResult<PatientModel>()
                {
                    ResourcePayload = new PatientModel()
                    {
                        Birthdate = DateTime.Parse("2000/01/31", CultureInfo.CurrentCulture),
                        FirstName = "Patient",
                        LastName = "Zero",
                        EmailAddress = "test@email.com",
                        HdId = hdid,
                        PersonalHealthNumber = phn,
                    },
                    ResultStatus = Common.Constants.ResultType.Success,
                }));

            Mock<IDrugLookupDelegate> drugLookupDelegateMock = new Mock<IDrugLookupDelegate>();

            // We need two tests, one for Fed data and one for Provincial data
            List<DrugProduct> drugList = new List<DrugProduct>()
            {
                new DrugProduct()
                {
                    DrugIdentificationNumber = din,
                    BrandName = "Brand Name",
                },
            };

            drugLookupDelegateMock.Setup(p => p.GetDrugProductsByDIN(It.IsAny<List<string>>())).Returns(drugList);
            Mock<IMedStatementDelegate> medStatementDelegateMock = new Mock<IMedStatementDelegate>();
            RequestResult<MedicationHistoryResponse> requestResult = new RequestResult<MedicationHistoryResponse>()
            {
                ResultStatus = Common.Constants.ResultType.Success,
                ResourcePayload = new MedicationHistoryResponse()
                {
                    TotalRecords = 1,
                    Pages = 1,
                    Results = new List<Models.ODR.MedicationResult>()
                    {
                        new Models.ODR.MedicationResult()
                        {
                            DIN = din,
                            GenericName = "Generic Name",
                        },
                    },
                },
            };
            medStatementDelegateMock.Setup(p => p.GetMedicationStatementsAsync(It.IsAny<OdrHistoryQuery>(), null, It.IsAny<string>(), ipAddress)).ReturnsAsync(requestResult);

            IMedicationStatementService service = new RestMedicationStatementService(
                new Mock<ILogger<RestMedicationStatementService>>().Object,
                httpContextAccessorMock.Object,
                patientDelegateMock.Object,
                drugLookupDelegateMock.Object,
                medStatementDelegateMock.Object);

            // Act
            RequestResult<IList<MedicationStatementHistory>> actual = Task.Run(async () => await service.GetMedicationStatementsHistory(hdid, null).ConfigureAwait(true)).Result;

            // Verify
            Assert.True(actual.ResultStatus == Common.Constants.ResultType.Success && actual?.ResourcePayload?.Count == 1);
        }

        [Fact]
        public void ShouldGetMedicationsProvLookup()
        {
            // Setup
            string hdid = "EXTRIOYFPNX35TWEBUAJ3DNFDFXSYTBC6J4M76GYE3HC5ER2NKWQ";
            string phn = "0009735353315";
            string userId = "1001";
            string ipAddress = "10.0.0.1";
            string din = "00000000";

            Mock<IIdentity> identityMock = new Mock<IIdentity>();
            identityMock.Setup(s => s.Name).Returns(userId);

            Mock<ClaimsPrincipal> claimsPrincipalMock = new Mock<ClaimsPrincipal>();
            claimsPrincipalMock.Setup(s => s.Identity).Returns(identityMock.Object);

            Mock<ConnectionInfo> connectionInfoMock = new Mock<ConnectionInfo>();
            connectionInfoMock.Setup(s => s.RemoteIpAddress).Returns(IPAddress.Parse(ipAddress));

            IHeaderDictionary headerDictionary = new HeaderDictionary();
            headerDictionary.Add("Authorization", "Bearer TestJWT");
            Mock<HttpRequest> httpRequestMock = new Mock<HttpRequest>();
            httpRequestMock.Setup(s => s.Headers).Returns(headerDictionary);

            Mock<HttpContext> httpContextMock = new Mock<HttpContext>();
            httpContextMock.Setup(s => s.Connection).Returns(connectionInfoMock.Object);
            httpContextMock.Setup(s => s.User).Returns(claimsPrincipalMock.Object);
            httpContextMock.Setup(s => s.Request).Returns(httpRequestMock.Object);

            Mock<IHttpContextAccessor> httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            httpContextAccessorMock.Setup(s => s.HttpContext).Returns(httpContextMock.Object);

            Mock<IPatientService> patientDelegateMock = new Mock<IPatientService>();
            patientDelegateMock.Setup(s => s.GetPatient(hdid, Common.Constants.PatientIdentifierType.HDID)).Returns(Task.FromResult(
                new RequestResult<PatientModel>()
                {
                    ResourcePayload = new PatientModel()
                    {
                        Birthdate = DateTime.Parse("2000/01/31", CultureInfo.CurrentCulture),
                        FirstName = "Patient",
                        LastName = "Zero",
                        EmailAddress = "test@email.com",
                        HdId = hdid,
                        PersonalHealthNumber = phn,
                    },
                    ResultStatus = Common.Constants.ResultType.Success,
                }));

            Mock<IDrugLookupDelegate> drugLookupDelegateMock = new Mock<IDrugLookupDelegate>();

            // We need two tests, one for Fed data and one for Provincial data
            List<PharmaCareDrug> drugList = new List<PharmaCareDrug>()
            {
                new PharmaCareDrug()
                {
                    DINPIN = din,
                    BrandName = "Brand Name",
                },
            };

            drugLookupDelegateMock.Setup(p => p.GetDrugProductsByDIN(It.IsAny<List<string>>())).Returns(new List<DrugProduct>());
            drugLookupDelegateMock.Setup(p => p.GetPharmaCareDrugsByDIN(It.IsAny<List<string>>())).Returns(drugList);

            Mock<IMedStatementDelegate> medStatementDelegateMock = new Mock<IMedStatementDelegate>();
            RequestResult<MedicationHistoryResponse> requestResult = new RequestResult<MedicationHistoryResponse>()
            {
                ResultStatus = Common.Constants.ResultType.Success,
                ResourcePayload = new MedicationHistoryResponse()
                {
                    TotalRecords = 1,
                    Pages = 1,
                    Results = new List<Models.ODR.MedicationResult>()
                    {
                        new Models.ODR.MedicationResult()
                        {
                            DIN = din,
                            GenericName = "Generic Name",
                        },
                    },
                },
            };
            medStatementDelegateMock.Setup(p => p.GetMedicationStatementsAsync(It.IsAny<OdrHistoryQuery>(), null, It.IsAny<string>(), ipAddress)).ReturnsAsync(requestResult);

            IMedicationStatementService service = new RestMedicationStatementService(
                new Mock<ILogger<RestMedicationStatementService>>().Object,
                httpContextAccessorMock.Object,
                patientDelegateMock.Object,
                drugLookupDelegateMock.Object,
                medStatementDelegateMock.Object);

            // Act
            RequestResult<IList<MedicationStatementHistory>> actual = Task.Run(async () => await service.GetMedicationStatementsHistory(hdid, null).ConfigureAwait(true)).Result;

            // Verify
            Assert.True(actual.ResultStatus == ResultType.Success && actual?.ResourcePayload?.Count == 1);
        }

        [Fact]
        public async Task ShouldGetEmptyMedications()
        {
            // Setup
            string hdid = "EXTRIOYFPNX35TWEBUAJ3DNFDFXSYTBC6J4M76GYE3HC5ER2NKWQ";
            string phn = "0009735353315";
            string userId = "1001";
            string ipAddress = "10.0.0.1";

            Mock<IIdentity> identityMock = new Mock<IIdentity>();
            identityMock.Setup(s => s.Name).Returns(userId);

            Mock<ClaimsPrincipal> claimsPrincipalMock = new Mock<ClaimsPrincipal>();
            claimsPrincipalMock.Setup(s => s.Identity).Returns(identityMock.Object);

            Mock<ConnectionInfo> connectionInfoMock = new Mock<ConnectionInfo>();
            connectionInfoMock.Setup(s => s.RemoteIpAddress).Returns(IPAddress.Parse(ipAddress));

            IHeaderDictionary headerDictionary = new HeaderDictionary();
            headerDictionary.Add("Authorization", "Bearer TestJWT");
            Mock<HttpRequest> httpRequestMock = new Mock<HttpRequest>();
            httpRequestMock.Setup(s => s.Headers).Returns(headerDictionary);

            Mock<HttpContext> httpContextMock = new Mock<HttpContext>();
            httpContextMock.Setup(s => s.Connection).Returns(connectionInfoMock.Object);
            httpContextMock.Setup(s => s.User).Returns(claimsPrincipalMock.Object);
            httpContextMock.Setup(s => s.Request).Returns(httpRequestMock.Object);

            Mock<IHttpContextAccessor> httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            httpContextAccessorMock.Setup(s => s.HttpContext).Returns(httpContextMock.Object);

            Mock<IPatientService> patientDelegateMock = new Mock<IPatientService>();
            patientDelegateMock.Setup(s => s.GetPatient(hdid, Common.Constants.PatientIdentifierType.HDID)).Returns(Task.FromResult(
                new RequestResult<PatientModel>()
                {
                    ResourcePayload = new PatientModel()
                    {
                        Birthdate = DateTime.Parse("2000/01/31", CultureInfo.CurrentCulture),
                        FirstName = "Patient",
                        LastName = "Zero",
                        EmailAddress = "test@email.com",
                        HdId = hdid,
                        PersonalHealthNumber = phn,
                    },
                    ResultStatus = Common.Constants.ResultType.Success,
                }));

            Mock<IDrugLookupDelegate> drugLookupDelegateMock = new Mock<IDrugLookupDelegate>();
            drugLookupDelegateMock.Setup(p => p.GetDrugProductsByDIN(It.IsAny<List<string>>())).Returns(new List<DrugProduct>());

            Mock<IMedStatementDelegate> medStatementDelegateMock = new Mock<IMedStatementDelegate>();
            RequestResult<MedicationHistoryResponse> requestResult = new RequestResult<MedicationHistoryResponse>()
            {
                ResultStatus = Common.Constants.ResultType.Success,
                ResourcePayload = new MedicationHistoryResponse()
                {
                    TotalRecords = 0,
                },
            };
            requestResult.ResourcePayload = new MedicationHistoryResponse();
            medStatementDelegateMock.Setup(p => p.GetMedicationStatementsAsync(It.IsAny<OdrHistoryQuery>(), null, It.IsAny<string>(), ipAddress)).ReturnsAsync(requestResult);

            IMedicationStatementService service = new RestMedicationStatementService(
                new Mock<ILogger<RestMedicationStatementService>>().Object,
                httpContextAccessorMock.Object,
                patientDelegateMock.Object,
                drugLookupDelegateMock.Object,
                medStatementDelegateMock.Object);

            // Act
            RequestResult<IList<MedicationStatementHistory>> actual = await service.GetMedicationStatementsHistory(hdid, null).ConfigureAwait(true);

            // Verify
            Assert.True(actual?.ResourcePayload?.Count == 0);
        }

        [Fact]
        public async Task ShouldGetPatientError()
        {
            // Setup
            string hdid = "EXTRIOYFPNX35TWEBUAJ3DNFDFXSYTBC6J4M76GYE3HC5ER2NKWQ";
            string userId = "1001";
            string ipAddress = "10.0.0.1";

            Mock<IIdentity> identityMock = new Mock<IIdentity>();
            identityMock.Setup(s => s.Name).Returns(userId);

            Mock<ClaimsPrincipal> claimsPrincipalMock = new Mock<ClaimsPrincipal>();
            claimsPrincipalMock.Setup(s => s.Identity).Returns(identityMock.Object);

            Mock<ConnectionInfo> connectionInfoMock = new Mock<ConnectionInfo>();
            connectionInfoMock.Setup(s => s.RemoteIpAddress).Returns(IPAddress.Parse(ipAddress));

            IHeaderDictionary headerDictionary = new HeaderDictionary();
            headerDictionary.Add("Authorization", "Bearer TestJWT");
            Mock<HttpRequest> httpRequestMock = new Mock<HttpRequest>();
            httpRequestMock.Setup(s => s.Headers).Returns(headerDictionary);

            Mock<HttpContext> httpContextMock = new Mock<HttpContext>();
            httpContextMock.Setup(s => s.Connection).Returns(connectionInfoMock.Object);
            httpContextMock.Setup(s => s.User).Returns(claimsPrincipalMock.Object);
            httpContextMock.Setup(s => s.Request).Returns(httpRequestMock.Object);

            Mock<IHttpContextAccessor> httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            httpContextAccessorMock.Setup(s => s.HttpContext).Returns(httpContextMock.Object);

            Mock<IPatientService> patientDelegateMock = new Mock<IPatientService>();
            patientDelegateMock.Setup(s => s.GetPatient(hdid, Common.Constants.PatientIdentifierType.HDID)).Returns(Task.FromResult(
                new RequestResult<PatientModel>()
                {
                    ResultStatus = Common.Constants.ResultType.Error,
                }));

            Mock<IDrugLookupDelegate> drugLookupDelegateMock = new Mock<IDrugLookupDelegate>();
            drugLookupDelegateMock.Setup(p => p.GetDrugProductsByDIN(It.IsAny<List<string>>())).Returns(new List<DrugProduct>());

            Mock<IMedStatementDelegate> medStatementDelegateMock = new Mock<IMedStatementDelegate>();
            RequestResult<MedicationHistoryResponse> requestResult = new RequestResult<MedicationHistoryResponse>()
            {
                ResultStatus = Common.Constants.ResultType.Success,
                ResourcePayload = new MedicationHistoryResponse()
                {
                    TotalRecords = 0,
                },
            };
            requestResult.ResourcePayload = new MedicationHistoryResponse();
            medStatementDelegateMock.Setup(p => p.GetMedicationStatementsAsync(It.IsAny<OdrHistoryQuery>(), null, It.IsAny<string>(), ipAddress)).ReturnsAsync(requestResult);

            IMedicationStatementService service = new RestMedicationStatementService(
                new Mock<ILogger<RestMedicationStatementService>>().Object,
                httpContextAccessorMock.Object,
                patientDelegateMock.Object,
                drugLookupDelegateMock.Object,
                medStatementDelegateMock.Object);

            // Act
            RequestResult<IList<MedicationStatementHistory>> actual = await service.GetMedicationStatementsHistory(hdid, null).ConfigureAwait(true);

            // Verify
            Assert.True(actual.ResultStatus == Common.Constants.ResultType.Error);
        }
    }
}
