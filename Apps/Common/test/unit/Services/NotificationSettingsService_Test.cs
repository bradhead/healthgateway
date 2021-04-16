// -------------------------------------------------------------------------
//  Copyright © 2019 Province of British Columbia
//
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//
//  http://www.apache.org/licenses/LICENSE-2.0
//
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.
// -------------------------------------------------------------------------
namespace HealthGateway.CommonTests.Services
{
    using System.Collections.Generic;
    using System.Text.Json;
    using System.Threading.Tasks;
    using DeepEqual.Syntax;
    using Hangfire;
    using Hangfire.Common;
    using Hangfire.States;
    using HealthGateway.Common.Delegates;
    using HealthGateway.Common.Models;
    using HealthGateway.Common.Services;
    using HealthGateway.Database.Delegates;
    using HealthGateway.Database.Models;
    using Microsoft.Extensions.Logging;
    using Moq;
    using Xunit;

    public class NotificationSettingsService_Test
    {
        private const string Hdid = "unit test hidid";

        [Fact]
        public void ShouldQueue()
        {
            NotificationSettingsRequest nsr = new NotificationSettingsRequest()
            {
                EmailEnabled = true,
                EmailAddress = "mock@mock.com",
                SMSEnabled = true,
                SMSNumber = "2505555555",
                SubjectHdid = "hdid",
                SMSVerificationCode = "123456",
                SMSVerified = false,
            };

            var mockLogger = new Mock<ILogger<NotificationSettingsService>>();
            var mockJobClient = new Mock<IBackgroundJobClient>();
            var mockNSDelegate = new Mock<INotificationSettingsDelegate>();
            var mockResourceDelegateDelegate = new Mock<IResourceDelegateDelegate>();
            var dbResult = new Database.Wrapper.DBResult<IEnumerable<ResourceDelegate>>();
            dbResult.Payload = new List<ResourceDelegate>();
            mockResourceDelegateDelegate.Setup(s => s.Get(nsr.SubjectHdid, 0, 500)).Returns(dbResult);
            INotificationSettingsService service = new NotificationSettingsService(
                                mockLogger.Object,
                                mockJobClient.Object,
                                mockNSDelegate.Object,
                                mockResourceDelegateDelegate.Object);

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                IgnoreNullValues = true,
                WriteIndented = true,
            };

            string expectedJobParm = JsonSerializer.Serialize(nsr, options);
            service.QueueNotificationSettings(nsr);

            mockJobClient.Verify(x => x.Create(
                    It.Is<Job>(job => job.Method.Name == "PushNotificationSettings" && (string)job.Args[0] == expectedJobParm),
                    It.IsAny<EnqueuedState>()));
        }

        [Fact]
        public void ShouldSend()
        {
            NotificationSettingsRequest nsRequest = new NotificationSettingsRequest()
            {
                EmailEnabled = true,
                EmailAddress = "mock@mock.com",
                SMSEnabled = false,
                SMSNumber = "2505555555",
                SubjectHdid = "hdid",
                SMSVerificationCode = "123456",
                SMSVerified = false,
            };
            string bearerToken = "bearer token";

            NotificationSettingsResponse nsResponse = new NotificationSettingsResponse(nsRequest);
            RequestResult<NotificationSettingsResponse> expectedResult = new RequestResult<NotificationSettingsResponse>()
            {
                ResourcePayload = nsResponse,
                ResultStatus = Common.Constants.ResultType.Success,
            };
            var mockLogger = new Mock<ILogger<NotificationSettingsService>>();
            var mockJobClient = new Mock<IBackgroundJobClient>();
            var mockNSDelegate = new Mock<INotificationSettingsDelegate>();
            var mockResourceDelegateDelegate = new Mock<IResourceDelegateDelegate>();
            mockNSDelegate.Setup(s => s.SetNotificationSettings(nsRequest, bearerToken)).Returns(Task.FromResult(expectedResult));
            INotificationSettingsService service = new NotificationSettingsService(
                                mockLogger.Object,
                                mockJobClient.Object,
                                mockNSDelegate.Object,
                                mockResourceDelegateDelegate.Object);
            RequestResult<NotificationSettingsResponse> actualResult = Task.Run(async () => await
                        service.SendNotificationSettings(nsRequest, bearerToken).ConfigureAwait(true)).Result;
            Assert.True(actualResult.IsDeepEqual(expectedResult));
        }

        [Fact]
        public void ShouldCreateSMSCode()
        {
            NotificationSettingsRequest nsr = new NotificationSettingsRequest()
            {
                EmailEnabled = true,
                EmailAddress = "mock@mock.com",
                SMSEnabled = true,
                SMSNumber = "2505555555",
                SubjectHdid = "hdid",
                SMSVerified = true,
            };

            var mockLogger = new Mock<ILogger<NotificationSettingsService>>();
            var mockJobClient = new Mock<IBackgroundJobClient>();
            var mockNSDelegate = new Mock<INotificationSettingsDelegate>();
            var mockResourceDelegateDelegate = new Mock<IResourceDelegateDelegate>();
            var dbResult = new Database.Wrapper.DBResult<IEnumerable<ResourceDelegate>>();
            dbResult.Payload = new List<ResourceDelegate>()
            {
                new ResourceDelegate()
                {
                    ProfileHdid = Hdid,
                },
            };
            mockResourceDelegateDelegate.Setup(s => s.Get(nsr.SubjectHdid, 0, 500)).Returns(dbResult);

            INotificationSettingsService service = new NotificationSettingsService(
                                mockLogger.Object,
                                mockJobClient.Object,
                                mockNSDelegate.Object,
                                mockResourceDelegateDelegate.Object);

            Assert.True(nsr.SMSVerificationCode == null);
            service.QueueNotificationSettings(nsr);

            mockJobClient.Verify(x => x.Create(
                    It.Is<Job>(job => job.Method.Name == "PushNotificationSettings" && job.Args[0] is string),
                    It.IsAny<EnqueuedState>()));

            Assert.True(nsr.SMSVerificationCode != null);
            Assert.True(nsr.SMSVerificationCode?.Length == 6);
        }
    }
}
