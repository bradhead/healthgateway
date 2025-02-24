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
namespace HealthGateway.GatewayApiTests.Services.Test
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using HealthGateway.Common.Constants;
    using HealthGateway.Common.Data.Constants;
    using HealthGateway.Common.Data.ErrorHandling;
    using HealthGateway.Common.Data.Models;
    using HealthGateway.Common.Delegates;
    using HealthGateway.Common.ErrorHandling;
    using HealthGateway.Common.ErrorHandling.Exceptions;
    using HealthGateway.Common.Messaging;
    using HealthGateway.Common.Models;
    using HealthGateway.Common.Services;
    using HealthGateway.Database.Constants;
    using HealthGateway.Database.Delegates;
    using HealthGateway.Database.Models;
    using HealthGateway.Database.Wrapper;
    using HealthGateway.GatewayApi.Models;
    using HealthGateway.GatewayApi.Services;
    using HealthGateway.GatewayApiTests.Utils;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using Moq;
    using Shouldly;
    using Xunit;

    /// <summary>
    /// DependentService's Unit Tests.
    /// </summary>
    public class DependentServiceTests
    {
        private static readonly IGatewayApiMappingService MappingService = new GatewayApiMappingService(MapperUtil.InitializeAutoMapper(), new Mock<ICryptoDelegate>().Object);

        private readonly string mismatchedError = "The information you entered does not match our records. Please try again.";
        private readonly string ineligibleError = "'Age' must be less than '12'.";
        private readonly DateTime mockDateOfBirth = DateTime.UtcNow.Date.AddYears(-12).AddDays(1);
        private readonly string mockFirstName = "Tory D'Bill"; // First name with regular apostrophe
        private readonly string mockGender = "Male";
        private readonly string mockHdId = "MockHdId";
        private readonly string mockLastName = "O'Neil"; // Last name with regular apostrophe
        private readonly string mockParentHdid = "MockFirstName";
        private readonly string mockPhn = "9735353315";
        private readonly string noHdidError = "Please ensure you are using a current BC Services Card.";
        private readonly int mockTotalDelegateCount = 2;
        private readonly DateTime fromDate = DateTime.UtcNow.AddDays(-1);
        private readonly DateTime toDate = DateTime.UtcNow.AddDays(1);

        public static TheoryData<RequestResult<PatientModel>, ResultType, string> InvalidPatientTheoryData => new()
        {
            { new(), ResultType.Error, "Communication Exception when trying to retrieve the Dependent" },
            { new() { ResultStatus = ResultType.ActionRequired, ResultError = new() { ActionCode = ActionType.NoHdId } }, ResultType.ActionRequired, ErrorMessages.InvalidServicesCard },
            { new() { ResultStatus = ResultType.ActionRequired }, ResultType.ActionRequired, ErrorMessages.DataMismatch },
            { new() { ResultStatus = ResultType.Success }, ResultType.ActionRequired, ErrorMessages.DataMismatch },
        };

        /// <summary>
        /// GetDependentsAsync by hdid - Happy Path.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task GetDependentsByHdid()
        {
            IDependentService service = this.SetupMockForGetDependents();

            RequestResult<IEnumerable<DependentModel>> actualResult = await service.GetDependentsAsync(this.mockParentHdid, 0, 25, It.IsAny<CancellationToken>());

            Assert.Equal(ResultType.Success, actualResult.ResultStatus);
            Assert.Equal(10, actualResult.TotalResultCount);

            Dictionary<string, int> delegateCounts = this.GenerateMockDelegateCounts();
            foreach (DependentModel model in actualResult.ResourcePayload!)
            {
                // Validate masked PHN
                Assert.Equal(this.mockPhn, model.DependentInformation.Phn);

                // Validate total delegate count
                Assert.Equal(delegateCounts[model.OwnerId], model.TotalDelegateCount);
            }
        }

        /// <summary>
        /// GetDependents by date - Happy Path.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task GetDependentsByDate()
        {
            IDependentService service = this.SetupMockForGetDependents();

            RequestResult<IEnumerable<GetDependentResponse>> actualResult = await service.GetDependentsAsync(this.fromDate, this.toDate, 0, 5000, It.IsAny<CancellationToken>());

            Assert.Equal(ResultType.Success, actualResult.ResultStatus);
            Assert.Equal(10, actualResult.TotalResultCount);

            // Validate masked PHN
            foreach (GetDependentResponse response in actualResult.ResourcePayload!)
            {
                Assert.Equal(response.DelegateId, this.mockParentHdid);
            }
        }

        /// <summary>
        /// GetDependentsAsync - Empty Patient Error.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task GetDependentsWithEmptyPatientResPayloadError()
        {
            RequestResult<PatientModel> patientResult = new();
            IDependentService service = this.SetupMockForGetDependents(patientResult);

            RequestResult<IEnumerable<DependentModel>> actualResult = await service.GetDependentsAsync(this.mockParentHdid);

            Assert.Equal(ResultType.Error, actualResult.ResultStatus);
            Assert.True(actualResult.ResultError?.ErrorCode.EndsWith("-CE-PAT", StringComparison.InvariantCulture));
            Assert.Equal(
                "Communication Exception when trying to retrieve Dependent(s) - HdId: MockHdId-0; HdId: MockHdId-1; HdId: MockHdId-2; HdId: MockHdId-3; HdId: MockHdId-4; HdId: MockHdId-5; HdId: MockHdId-6; HdId: MockHdId-7; HdId: MockHdId-8; HdId: MockHdId-9;",
                actualResult.ResultError?.ResultMessage);
        }

        /// <summary>
        /// AddDependentAsync - Happy Path.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task ValidateAddDependent()
        {
            AddDependentRequest addDependentRequest = this.SetupMockInput();
            IDependentService service = this.SetupMockDependentService(addDependentRequest);

            RequestResult<DependentModel> actualResult = await service.AddDependentAsync(this.mockParentHdid, addDependentRequest);

            Assert.Equal(ResultType.Success, actualResult.ResultStatus);
            Dictionary<string, int> delegateCounts = this.GenerateMockDelegateCounts(true);
            Assert.Equal(delegateCounts[actualResult.ResourcePayload?.OwnerId], actualResult.ResourcePayload?.TotalDelegateCount);
        }

        /// <summary>
        /// AddDependentAsync - Ensure names with smart quotes are transformed to regular quotes and validated.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task ValidateAddDependentWithNamesContainingSmartApostrophe()
        {
            AddDependentRequest addDependentRequest = this.SetupMockInput();
            addDependentRequest.FirstName = "Tory D’Bill"; // First name with smart apostrophe
            addDependentRequest.LastName = "O’Neil"; // Last name with smart apostrophe
            IDependentService service = this.SetupMockDependentService(addDependentRequest);

            RequestResult<DependentModel> actualResult = await service.AddDependentAsync(this.mockParentHdid, addDependentRequest);

            Assert.Equal(ResultType.Success, actualResult.ResultStatus);
            Dictionary<string, int> delegateCounts = this.GenerateMockDelegateCounts(true);
            Assert.Equal(delegateCounts[actualResult.ResourcePayload?.OwnerId], actualResult.ResourcePayload?.TotalDelegateCount);
        }

        /// <summary>
        /// AddDependentAsync - Invalid Patient Response.
        /// </summary>
        /// <param name="patient">Response from retrieving patient.</param>
        /// <param name="expectedResultType">Expected result type.</param>
        /// <param name="expectedErrorMessage">Expected error message.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Theory]
        [MemberData(nameof(InvalidPatientTheoryData))]
        public async Task ValidateAddDependentWithInvalidPatient(RequestResult<PatientModel> patient, ResultType expectedResultType, string expectedErrorMessage)
        {
            AddDependentRequest addDependentRequest = this.SetupMockInput();
            IDependentService service = this.SetupMockDependentService(addDependentRequest, null, patient);

            RequestResult<DependentModel> actualResult = await service.AddDependentAsync(this.mockParentHdid, addDependentRequest);

            Assert.Equal(expectedResultType, actualResult.ResultStatus);
            Assert.NotNull(actualResult.ResultError);
            Assert.Equal(expectedErrorMessage, actualResult.ResultError.ResultMessage);
        }

        /// <summary>
        /// AddDependentAsync - Database Error.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task ValidateAddDependentWithDbError()
        {
            DbResult<ResourceDelegate> insertResult = new()
            {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
                Payload = null,
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
                Status = DbStatusCode.Error,
            };
            AddDependentRequest addDependentRequest = this.SetupMockInput();
            IDependentService service = this.SetupMockDependentService(addDependentRequest, insertResult);

            DatabaseException exception =
                (await Should.ThrowAsync<DatabaseException>(async () => await service.AddDependentAsync(this.mockParentHdid, addDependentRequest)))
                .ShouldNotBeNull();

            exception.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        }

        /// <summary>
        /// AddDependentAsync - Ineligible Due to Validation.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task ValidateAddDependentIneligible()
        {
            AddDependentRequest addDependentRequest = this.SetupMockInput();
            addDependentRequest.DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-20));
            IDependentService service = this.SetupMockDependentService(addDependentRequest);

            RequestResult<DependentModel> actualResult = await service.AddDependentAsync(this.mockParentHdid, addDependentRequest);

            RequestResultError userError = ErrorTranslator.ActionRequired(ErrorMessages.DataMismatch, ActionType.DataMismatch);
            Assert.Equal(ResultType.Error, actualResult.ResultStatus);
            Assert.Equal(userError.ErrorCode, actualResult.ResultError!.ErrorCode);
            Assert.Equal(this.ineligibleError, actualResult.ResultError.ResultMessage);
        }

        /// <summary>
        /// AddDependentAsync - Wrong First Name Error.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task ValidateAddDependentWithWrongFirstName()
        {
            AddDependentRequest addDependentRequest = this.SetupMockInput();
            addDependentRequest.FirstName = "wrong";
            IDependentService service = this.SetupMockDependentService(addDependentRequest);

            RequestResult<DependentModel> actualResult = await service.AddDependentAsync(this.mockParentHdid, addDependentRequest);

            RequestResultError userError = ErrorTranslator.ActionRequired(ErrorMessages.DataMismatch, ActionType.DataMismatch);
            Assert.Equal(ResultType.ActionRequired, actualResult.ResultStatus);
            Assert.Equal(userError.ErrorCode, actualResult.ResultError!.ErrorCode);
            Assert.Equal(this.mismatchedError, actualResult.ResultError.ResultMessage);
        }

        /// <summary>
        /// AddDependentAsync - Wrong Last Name Error.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task ValidateAddDependentWithWrongLastName()
        {
            AddDependentRequest addDependentRequest = this.SetupMockInput();
            addDependentRequest.LastName = "wrong";
            IDependentService service = this.SetupMockDependentService(addDependentRequest);

            RequestResult<DependentModel> actualResult = await service.AddDependentAsync(this.mockParentHdid, addDependentRequest);

            RequestResultError userError = ErrorTranslator.ActionRequired(ErrorMessages.DataMismatch, ActionType.DataMismatch);
            Assert.Equal(ResultType.ActionRequired, actualResult.ResultStatus);
            Assert.Equal(userError.ErrorCode, actualResult.ResultError!.ErrorCode);
            Assert.Equal(this.mismatchedError, actualResult.ResultError.ResultMessage);
        }

        /// <summary>
        /// AddDependentAsync - Wrong Date of Birth Error.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task ValidateAddDependentWithWrongDateOfBirth()
        {
            AddDependentRequest addDependentRequest = this.SetupMockInput();
            addDependentRequest.DateOfBirth = DateOnly.FromDateTime(DateTime.Now);
            IDependentService service = this.SetupMockDependentService(addDependentRequest);

            RequestResult<DependentModel> actualResult = await service.AddDependentAsync(this.mockParentHdid, addDependentRequest);

            RequestResultError userError = ErrorTranslator.ActionRequired(ErrorMessages.DataMismatch, ActionType.DataMismatch);
            Assert.Equal(ResultType.ActionRequired, actualResult.ResultStatus);
            Assert.Equal(userError.ErrorCode, actualResult.ResultError!.ErrorCode);
            Assert.Equal(this.mismatchedError, actualResult.ResultError.ResultMessage);
        }

        /// <summary>
        /// AddDependentAsync - No HdId Error.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task ValidateAddDependentWithNoHdId()
        {
            RequestResult<PatientModel> patientResult = new()
            {
                ResultStatus = ResultType.Success,
                ResourcePayload = new PatientModel
                {
                    HdId = string.Empty,
                    PersonalHealthNumber = this.mockPhn,
                    FirstName = this.mockFirstName,
                    LastName = this.mockLastName,
                    Birthdate = this.mockDateOfBirth,
                    Gender = this.mockGender,
                },
            };
            AddDependentRequest addDependentRequest = this.SetupMockInput();
            IDependentService service = this.SetupMockDependentService(addDependentRequest, patientResult: patientResult);

            RequestResult<DependentModel> actualResult = await service.AddDependentAsync(this.mockParentHdid, addDependentRequest);

            RequestResultError userError = ErrorTranslator.ActionRequired(ErrorMessages.InvalidServicesCard, ActionType.NoHdId);
            Assert.Equal(ResultType.ActionRequired, actualResult.ResultStatus);
            Assert.Equal(userError.ErrorCode, actualResult.ResultError!.ErrorCode);
            Assert.Equal(this.noHdidError, actualResult.ResultError.ResultMessage);
        }

        /// <summary>
        /// AddDependentAsync - Happy Path (Protected).
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task ValidateAddProtectedDependent()
        {
            AddDependentRequest addDependentRequest = this.SetupMockInput();
            Dependent dependent = new()
            {
                HdId = this.mockHdId,
                Protected = true,
                AllowedDelegations = [new() { DependentHdId = this.mockHdId, DelegateHdId = this.mockParentHdid }],
            };
            IDependentService service = this.SetupMockDependentService(addDependentRequest, dependent: dependent);

            RequestResult<DependentModel> actualResult = await service.AddDependentAsync(this.mockParentHdid, addDependentRequest);

            Assert.Equal(ResultType.Success, actualResult.ResultStatus);
        }

        /// <summary>
        /// AddDependentAsync - Error (Protected).
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task ValidateAddProtectedDependentNotAllowed()
        {
            AddDependentRequest addDependentRequest = this.SetupMockInput();
            Dependent dependent = new()
            {
                HdId = this.mockHdId,
                Protected = true,
            };
            IDependentService service = this.SetupMockDependentService(addDependentRequest, dependent: dependent);

            RequestResult<DependentModel> actualResult = await service.AddDependentAsync(this.mockParentHdid, addDependentRequest);

            Assert.Equal(ResultType.ActionRequired, actualResult.ResultStatus);
            Assert.Equal(ActionType.Protected, actualResult.ResultError!.ActionCode);
        }

        [Fact]
        public async Task ValidateRemove()
        {
            DependentModel delegateModel = new() { OwnerId = this.mockHdId, DelegateId = this.mockParentHdid };
            IList<ResourceDelegate> resourceDelegates = new[] { new ResourceDelegate { ProfileHdid = this.mockParentHdid, ResourceOwnerHdid = this.mockHdId } };
            DbResult<ResourceDelegate> dbResult = new()
            {
                Status = DbStatusCode.Deleted,
            };
            IDependentService service = this.SetupMockForRemoveDependent(resourceDelegates, dbResult);

            RequestResult<DependentModel> actualResult = await service.RemoveAsync(delegateModel);

            Assert.Equal(ResultType.Success, actualResult.ResultStatus);
        }

        [Fact]
        public async Task ValidateRemoveNotFound()
        {
            DependentModel delegateModel = new() { OwnerId = this.mockHdId, DelegateId = this.mockParentHdid };
            IDependentService service = this.SetupMockForRemoveDependent([], new());

            async Task Actual()
            {
                await service.RemoveAsync(delegateModel);
            }

            await Assert.ThrowsAsync<NotFoundException>(Actual);
        }

        [Fact]
        public async Task ValidateRemoveDatabaseException()
        {
            DependentModel delegateModel = new() { OwnerId = this.mockHdId, DelegateId = this.mockParentHdid };
            IList<ResourceDelegate> resourceDelegates = new[] { new ResourceDelegate { ProfileHdid = this.mockParentHdid, ResourceOwnerHdid = this.mockHdId } };
            DbResult<ResourceDelegate> dbResult = new()
            {
                Status = DbStatusCode.Error,
            };
            IDependentService service = this.SetupMockForRemoveDependent(resourceDelegates, dbResult);

            async Task Actual()
            {
                await service.RemoveAsync(delegateModel);
            }

            await Assert.ThrowsAsync<DatabaseException>(Actual);
        }

        private static IConfigurationRoot GetIConfigurationRoot(Dictionary<string, string?>? localConfig)
        {
            Dictionary<string, string?> myConfiguration = localConfig ?? [];

            return new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", true)
                .AddJsonFile("appsettings.Development.json", true)
                .AddJsonFile("appsettings.local.json", true)
                .AddInMemoryCollection(myConfiguration.ToList())
                .Build();
        }

        private IList<ResourceDelegate> GenerateMockResourceDelegatesList()
        {
            List<ResourceDelegate> resourceDelegates = [];

            for (int i = 0; i < 10; i++)
            {
                resourceDelegates.Add(
                    new ResourceDelegate
                    {
                        ProfileHdid = this.mockParentHdid,
                        ResourceOwnerHdid = this.GetMockDependentHdid(i),
                    });
            }

            return resourceDelegates;
        }

        private string GetMockDependentHdid(int? index = null)
        {
            return index == null ? this.mockHdId : $"{this.mockHdId}-{index}";
        }

        private int GetMockTotalResultCount(int? index = null)
        {
            return index == null ? this.mockTotalDelegateCount : this.mockTotalDelegateCount + index.Value;
        }

        private Dictionary<string, int> GenerateMockDelegateCounts(bool single = false)
        {
            Dictionary<string, int> delegateCounts = [];

            if (single)
            {
                delegateCounts.Add(this.GetMockDependentHdid(), this.GetMockTotalResultCount());
            }
            else
            {
                for (int i = 0; i < 10; i++)
                {
                    delegateCounts.Add(this.GetMockDependentHdid(i), this.GetMockTotalResultCount(i));
                }
            }

            return delegateCounts;
        }

        private IDependentService SetupMockForGetDependents(RequestResult<PatientModel>? patientResult = null)
        {
            // Setup Mock<IResourceDelegateDelegate>
            DbResult<Dictionary<string, int>> mockDelegateCountsResult = new()
            {
                Status = DbStatusCode.Read,
                Payload = this.GenerateMockDelegateCounts(),
            };

            Mock<IResourceDelegateDelegate> mockResourceDelegateDelegate = new();
            mockResourceDelegateDelegate.Setup(s => s.GetAsync(this.mockParentHdid, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(this.GenerateMockResourceDelegatesList());
            mockResourceDelegateDelegate.Setup(s => s.GetAsync(this.fromDate, this.toDate, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(this.GenerateMockResourceDelegatesList());
            mockResourceDelegateDelegate.Setup(
                    s => s.GetTotalDelegateCountsAsync(It.Is<IEnumerable<string>>(h => h.SequenceEqual(mockDelegateCountsResult.Payload.Keys)), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockDelegateCountsResult)
                .Verifiable();

            // Setup Mock<IPatientService>
            Mock<IPatientService> mockPatientService = new();

            patientResult ??= new RequestResult<PatientModel>
            {
                ResultStatus = ResultType.Success,
                ResourcePayload = new PatientModel
                {
                    HdId = this.mockHdId,
                    PersonalHealthNumber = this.mockPhn,
                    FirstName = this.mockFirstName,
                    LastName = this.mockLastName,
                    Birthdate = this.mockDateOfBirth,
                    Gender = this.mockGender,
                },
            };

            mockPatientService.Setup(s => s.GetPatientAsync(It.IsAny<string>(), It.IsAny<PatientIdentifierType>(), false, It.IsAny<CancellationToken>())).ReturnsAsync(patientResult);

            Mock<IMessageSender> mockMessageSender = new();
            mockMessageSender.Setup(s => s.SendAsync(It.IsAny<IEnumerable<MessageEnvelope>>(), It.IsAny<CancellationToken>()));

            // Setup other common Mocks
            Mock<IDelegationDelegate> mockDelegationDelegate = new();
            Mock<IUserProfileDelegate> mockUserProfileDelegate = new();
            Mock<INotificationSettingsService> mockNotificationSettingsService = new();

            return new DependentService(
                GetIConfigurationRoot(null),
                new Mock<ILogger<DependentService>>().Object,
                mockPatientService.Object,
                mockNotificationSettingsService.Object,
                mockDelegationDelegate.Object,
                mockResourceDelegateDelegate.Object,
                mockUserProfileDelegate.Object,
                mockMessageSender.Object,
                MappingService);
        }

        private IDependentService SetupMockForRemoveDependent(IList<ResourceDelegate> resourceDelegates, DbResult<ResourceDelegate> dbResult)
        {
            Mock<IResourceDelegateDelegate> mockResourceDelegateDelegate = new();
            mockResourceDelegateDelegate.Setup(s => s.GetAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(resourceDelegates);
            mockResourceDelegateDelegate.Setup(s => s.DeleteAsync(It.IsAny<ResourceDelegate>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(dbResult);

            Mock<IUserProfileDelegate> mockUserProfileDelegate = new();
            mockUserProfileDelegate.Setup(s => s.GetUserProfileAsync(this.mockParentHdid, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new UserProfile());
            Mock<INotificationSettingsService> mockNotificationSettingsService = new();
            mockNotificationSettingsService.Setup(s => s.QueueNotificationSettingsAsync(It.IsAny<NotificationSettingsRequest>(), It.IsAny<CancellationToken>()));

            Mock<IMessageSender> mockMessageSender = new();
            mockMessageSender.Setup(s => s.SendAsync(It.IsAny<IEnumerable<MessageEnvelope>>(), It.IsAny<CancellationToken>()));

            return new DependentService(
                GetIConfigurationRoot(null),
                new Mock<ILogger<DependentService>>().Object,
                new Mock<IPatientService>().Object,
                mockNotificationSettingsService.Object,
                new Mock<IDelegationDelegate>().Object,
                mockResourceDelegateDelegate.Object,
                mockUserProfileDelegate.Object,
                mockMessageSender.Object,
                MappingService);
        }

        private IDependentService SetupMockDependentService(
            AddDependentRequest addDependentRequest,
            DbResult<ResourceDelegate>? insertResult = null,
            RequestResult<PatientModel>? patientResult = null,
            Dependent? dependent = null)
        {
            Mock<IPatientService> mockPatientService = new();

            RequestResult<string> patientHdIdResult = new()
            {
                ResultStatus = ResultType.Success,
                ResourcePayload = this.mockHdId,
            };
            if (addDependentRequest.Phn.Equals(this.mockPhn, StringComparison.Ordinal))
            {
                // Test Scenario - Happy Path: HiId found for the mockPHN
                patientHdIdResult.ResultStatus = ResultType.Success;
                patientHdIdResult.ResourcePayload = this.mockHdId;
            }

            // Test Scenario - Happy Path: Found HdId for the PHN, Found Patient.
            patientResult ??= new RequestResult<PatientModel>
            {
                ResultStatus = ResultType.Success,
                ResourcePayload = new PatientModel
                {
                    HdId = this.mockHdId,
                    PersonalHealthNumber = this.mockPhn,
                    FirstName = this.mockFirstName,
                    LastName = this.mockLastName,
                    Birthdate = this.mockDateOfBirth,
                    Gender = this.mockGender,
                },
            };

            mockPatientService.Setup(s => s.GetPatientAsync(It.IsAny<string>(), It.IsAny<PatientIdentifierType>(), false, It.IsAny<CancellationToken>())).ReturnsAsync(patientResult);

            ResourceDelegate expectedDbDependent = new() { ProfileHdid = this.mockParentHdid, ResourceOwnerHdid = this.mockHdId };

            insertResult ??= new DbResult<ResourceDelegate>
            {
                Status = DbStatusCode.Created,
            };

            insertResult.Payload = expectedDbDependent;

            Mock<IResourceDelegateDelegate> mockDependentDelegate = new();
            mockDependentDelegate.Setup(
                    s => s.InsertAsync(
                        It.Is<ResourceDelegate>(r => r.ProfileHdid == expectedDbDependent.ProfileHdid && r.ResourceOwnerHdid == expectedDbDependent.ResourceOwnerHdid),
                        It.IsAny<bool>(),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(insertResult);
            DbResult<Dictionary<string, int>> mockDelegateCountsResult = new()
            {
                Status = DbStatusCode.Read,
                Payload = this.GenerateMockDelegateCounts(true),
            };
            mockDependentDelegate.Setup(s => s.GetTotalDelegateCountsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>())).ReturnsAsync(mockDelegateCountsResult);

            Mock<IDelegationDelegate> delegationDelegate = new();
            delegationDelegate.Setup(s => s.GetDependentAsync(this.mockHdId, true, CancellationToken.None)).ReturnsAsync(dependent);

            Mock<IUserProfileDelegate> mockUserProfileDelegate = new();
            mockUserProfileDelegate.Setup(s => s.GetUserProfileAsync(this.mockParentHdid, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new UserProfile());

            Mock<INotificationSettingsService> mockNotificationSettingsService = new();
            mockNotificationSettingsService.Setup(s => s.QueueNotificationSettingsAsync(It.IsAny<NotificationSettingsRequest>(), It.IsAny<CancellationToken>()));

            Mock<IMessageSender> mockMessageSender = new();
            mockMessageSender.Setup(s => s.SendAsync(It.IsAny<IEnumerable<MessageEnvelope>>(), It.IsAny<CancellationToken>()));

            return new DependentService(
                GetIConfigurationRoot(null),
                new Mock<ILogger<DependentService>>().Object,
                mockPatientService.Object,
                mockNotificationSettingsService.Object,
                delegationDelegate.Object,
                mockDependentDelegate.Object,
                mockUserProfileDelegate.Object,
                mockMessageSender.Object,
                MappingService);
        }

        private AddDependentRequest SetupMockInput()
        {
            return new AddDependentRequest
            {
                Phn = this.mockPhn,
                FirstName = this.mockFirstName,
                LastName = this.mockLastName,
                DateOfBirth = DateOnly.FromDateTime(this.mockDateOfBirth),
            };
        }
    }
}
