﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace NotificationService.UnitTests.BusinessLibrary.Providers
{
    using System.Net.Http;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Options;
    using Moq;
    using NotificationService.BusinessLibrary;
    using NotificationService.Common;
    using NotificationService.Common.Logger;

    public class MSGraphProvideTestBase
    {
        private readonly string sendEmailUrl = "v1/sendEmail";

        /// <summary>
        /// Gets Test Application name.
        /// </summary>
        public string ApplicationName => "TestApp";

        /// <summary>
        /// Gets or sets MSGraphSetting Configuration Mock.
        /// </summary>
        public IOptions<MSGraphSetting> MsGraphSetting { get; set; }

        /// <summary>
        /// Gets or sets Logger.
        /// </summary>
        public ILogger Logger { get; set; }

        /// <summary>
        /// Gets or sets Logger.
        /// </summary>
        public Mock<HttpClient> MockedHttpClient { get; set; }

        /// <summary>
        /// Gets or sets Configuration.
        /// </summary>
        public IConfiguration Configuration { get; set; }

        protected void SetupTestBase()
        {
            this.MsGraphSetting = Options.Create(new MSGraphSetting() { EnableBatching = false, SendMailUrl = this.sendEmailUrl, BatchRequestLimit = 4 });
            this.Logger = new Mock<ILogger>().Object;
            this.MockedHttpClient = new Mock<HttpClient>();
        }
    }
}
