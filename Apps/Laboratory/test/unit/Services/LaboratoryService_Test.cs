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
namespace HealthGateway.LaboratoryTests
{
    using HealthGateway.Common.Models;
    using HealthGateway.Laboratory.Models;
    using HealthGateway.Laboratory.Services;
    using Moq;
    using System.Collections.Generic;
    using Microsoft.AspNetCore.Http;
    using System.Threading.Tasks;
    using Xunit;
    using Microsoft.Extensions.Logging;
    using System;
    using HealthGateway.Laboratory.Delegates;
    using System.Net;
    using HealthGateway.Laboratory.Factories;

    public class LaboratoryService_Test
    {
        private const string bearerToken = "mockBearerToken123";
        private const string ipAddress = "127.0.0.1";
        private const string mockedMessageID = "mockedMessageID";
        private const string mockedReportContent = "mockedReportContent";

        private ILaboratoryService GetLabServiceForLabOrdersTests(Common.Constants.ResultType expectedResultType)
        {

            List<LaboratoryOrder> labOrders = new List<LaboratoryOrder>()
            {
                new LaboratoryOrder()
                {
                    Id = Guid.NewGuid(),
                    Location = "Vancouver",
                    PHN = "001",
                    MessageDateTime = DateTime.Now,
                    MessageID = mockedMessageID + "1",
                    ReportAvailable = true,
                },
                new LaboratoryOrder()
                {
                    Id = Guid.NewGuid(),
                    Location = "Vancouver",
                    PHN = "002",
                    MessageDateTime = DateTime.Now,
                    MessageID = mockedMessageID + "2",
                    ReportAvailable = false,
                },
            };
            RequestResult<IEnumerable<LaboratoryOrder>> delegateResult = new RequestResult<IEnumerable<LaboratoryOrder>>()
            {
                ResultStatus = expectedResultType,
                PageSize = 100,
                PageIndex = 1,
                ResourcePayload = labOrders,
            };

            var mockLaboratoryDelegate = new Mock<ILaboratoryDelegate>();
            mockLaboratoryDelegate.Setup(s => s.GetLaboratoryOrders(It.IsAny<string>(), It.IsAny<int>())).Returns(Task.FromResult(delegateResult));

            var mockLaboratoryDelegateFactory = new Mock<ILaboratoryDelegateFactory>();
            mockLaboratoryDelegateFactory.Setup(s => s.CreateInstance()).Returns(mockLaboratoryDelegate.Object);

            var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            var context = new DefaultHttpContext()
            {
                Connection =
                {
                    RemoteIpAddress = IPAddress.Parse(ipAddress),
                },
            };
            context.Request.Headers.Add("Authorization", "MockJWTHeader");
            mockHttpContextAccessor.Setup(_ => _.HttpContext).Returns(context);

            ILaboratoryService service = new LaboratoryService(new Mock<ILogger<LaboratoryService>>().Object,
                                                             mockHttpContextAccessor.Object,
                                                             mockLaboratoryDelegateFactory.Object);
            return service;
        }

        [Fact]
        public void GetLabOrders()
        {
            var service = GetLabServiceForLabOrdersTests(Common.Constants.ResultType.Success);
            var actualResult = service.GetLaboratoryOrders(bearerToken, 0);

            List<LaboratoryOrder> resultLabOrders = actualResult.Result.ResourcePayload as List<LaboratoryOrder>;
            Assert.True(actualResult.Result.ResultStatus == Common.Constants.ResultType.Success);
            var count = 0;
            foreach (LaboratoryModel model in actualResult.Result.ResourcePayload)
            {
                count++;
                Assert.True(model.MessageID.Equals(mockedMessageID + count));
            };
            Assert.True(count == 2);
        }

        [Fact]
        public void GetLabOrdersWithError()
        {
            var service = GetLabServiceForLabOrdersTests(Common.Constants.ResultType.Error);
            var actualResult = service.GetLaboratoryOrders(bearerToken, 0);
            Assert.True(actualResult.Result.ResultStatus == Common.Constants.ResultType.Error);
        }

        [Fact]
        public void GetLabReport()
        {
            LaboratoryReport labReport = new LaboratoryReport()
            {
                Report = mockedReportContent,
                MediaType = "mockedMediaType",
                Encoding = "mockedEncoding"
            };
            RequestResult<LaboratoryReport> delegateResult = new RequestResult<LaboratoryReport>()
            {
                ResultStatus = Common.Constants.ResultType.Success,
                PageSize = 100,
                PageIndex = 1,
                ResourcePayload = labReport
            };

            var mockLaboratoryDelegate = new Mock<ILaboratoryDelegate>();
            mockLaboratoryDelegate.Setup(s => s.GetLabReport(It.IsAny<Guid>(), It.IsAny<string>())).Returns(Task.FromResult(delegateResult));

            var mockLaboratoryDelegateFactory = new Mock<ILaboratoryDelegateFactory>();
            mockLaboratoryDelegateFactory.Setup(s => s.CreateInstance()).Returns(mockLaboratoryDelegate.Object);

            var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            var context = new DefaultHttpContext()
            {
                Connection =
                {
                    RemoteIpAddress = IPAddress.Parse(ipAddress),
                },
            };
            context.Request.Headers.Add("Authorization", "MockJWTHeader");
            mockHttpContextAccessor.Setup(_ => _.HttpContext).Returns(context);

            ILaboratoryService service = new LaboratoryService(new Mock<ILogger<LaboratoryService>>().Object,
                                                             mockHttpContextAccessor.Object,
                                                             mockLaboratoryDelegateFactory.Object);
            var actualResult = service.GetLabReport(Guid.NewGuid(), bearerToken);

            Assert.True(actualResult.Result.ResultStatus == Common.Constants.ResultType.Success);
            Assert.True(actualResult.Result.ResourcePayload.Report == mockedReportContent);
        }
    }
}
