﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace NotificationService.BusinessLibrary.Trackers
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using Microsoft.Extensions.Logging;
    using NotificationService.Contracts.Models.Trackers;

    // TODO : To be removed later along with unit tests.

    /// <summary>
    /// The <see cref="UserConnectionTracker"/> class tracks the user connection information in-memory.
    /// </summary>
    /// <seealso cref="IUserConnectionTracker" />
    public class UserConnectionTracker : IUserConnectionTracker, IUserConnectionsReader
    {
        /// <summary>
        /// The browser application.
        /// </summary>
        public const string BrowserApplication = "Browser";

        /// <summary>
        /// The user connections.
        /// </summary>
        private readonly ConcurrentDictionary<string, HashSet<UserConnectionInfo>> connections;

        /// <summary>
        /// The logger.
        /// </summary>
        private readonly ILogger<UserConnectionTracker> logger;

        /// <summary>
        /// The lock object.
        /// </summary>
        private readonly object lockObject;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserConnectionTracker"/> class (DI usage: Singleton only).
        /// </summary>
        /// <remarks>
        /// DI usage: Singleton only.
        /// </remarks>
        /// <param name="logger">The instance of <see cref="ILogger{UserConnectionTracker}"/>.</param>
        /// <exception cref="ArgumentNullException">logger.</exception>
        public UserConnectionTracker(ILogger<UserConnectionTracker> logger)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.connections = new ConcurrentDictionary<string, HashSet<UserConnectionInfo>>();
            this.lockObject = new object();
        }

        /// <inheritdoc />
        public HashSet<UserConnectionInfo> RetrieveConnectionInfo(string userObjectIdentifier)
        {
            HashSet<UserConnectionInfo> userConnectionsSet = null;
            this.logger.LogInformation($"Started {nameof(this.RetrieveConnectionInfo)} method of {nameof(UserConnectionTracker)}.");
            if (string.IsNullOrWhiteSpace(userObjectIdentifier))
            {
                throw new ArgumentException("The user object identifier is not specified.", nameof(userObjectIdentifier));
            }

            _ = this.connections.TryGetValue(userObjectIdentifier, out userConnectionsSet);
            this.logger.LogInformation($"Finished {nameof(this.RetrieveConnectionInfo)} method of {nameof(UserConnectionTracker)}.");
            return userConnectionsSet;
        }

        /// <inheritdoc />
        public void SetConnectionInfo(string userObjectIdentifier, UserConnectionInfo userConnectionInfo)
        {
            this.logger.LogInformation($"Started {nameof(this.SetConnectionInfo)} method of {nameof(UserConnectionTracker)}.");
            if (string.IsNullOrWhiteSpace(userObjectIdentifier))
            {
                throw new ArgumentException("The user object identifier is not specified.", nameof(userObjectIdentifier));
            }

            if (userConnectionInfo == null)
            {
                throw new ArgumentNullException(nameof(userConnectionInfo));
            }

            this.AddConnection(userObjectIdentifier, userConnectionInfo);
            this.logger.LogInformation($"Finished {nameof(this.SetConnectionInfo)} method of {nameof(UserConnectionTracker)}.");
        }

        /// <inheritdoc />
        public void RemoveConnectionInfo(string userObjectIdentifier, string connectionId)
        {
            this.logger.LogInformation($"Started {nameof(this.RemoveConnectionInfo)} method of {nameof(UserConnectionTracker)}.");
            if (string.IsNullOrWhiteSpace(userObjectIdentifier))
            {
                throw new ArgumentException("The user object identifier is not specified.", nameof(userObjectIdentifier));
            }

            if (string.IsNullOrWhiteSpace(connectionId))
            {
                throw new ArgumentException("The connection Id is not specified.", nameof(connectionId));
            }

            this.RemoveConnectionInternal(userObjectIdentifier, connectionId);
            this.logger.LogInformation($"Finished {nameof(this.RemoveConnectionInfo)} method of {nameof(UserConnectionTracker)}.");
        }

        /// <inheritdoc />
        public void SetConnectionApplicationName(string userObjectIdentifier, UserConnectionInfo userConnectionInfo)
        {
            this.logger.LogInformation($"Started {nameof(this.SetConnectionApplicationName)} method of {nameof(UserConnectionTracker)}.");
            if (string.IsNullOrWhiteSpace(userObjectIdentifier))
            {
                throw new ArgumentException("The user object identifier is not specified.", nameof(userObjectIdentifier));
            }

            if (userConnectionInfo == null)
            {
                throw new ArgumentNullException(nameof(userConnectionInfo));
            }

            this.SetApplicationNameInternal(userObjectIdentifier, userConnectionInfo);
        }

        /// <inheritdoc />
        public IEnumerable<string> GetUserConnectionIds(string userObjectIdentifier, string applicationName)
        {
            IEnumerable<string> connectionIds;
            this.logger.LogInformation($"Started {nameof(this.GetUserConnectionIds)} method of {nameof(UserConnectionTracker)}.");
            if (string.IsNullOrWhiteSpace(userObjectIdentifier))
            {
                throw new ArgumentException("The user object identifier is not specified.", nameof(userObjectIdentifier));
            }

            if (string.IsNullOrWhiteSpace(applicationName))
            {
                throw new ArgumentException("The application name is not specified.", nameof(applicationName));
            }

            connectionIds = this.GetConnectionIdsInternal(userObjectIdentifier, applicationName);
            this.logger.LogInformation($"Finished {nameof(this.GetUserConnectionIds)} method of {nameof(UserConnectionTracker)}.");
            return connectionIds;
        }

        /// <summary>
        /// Gets the connection ids internally for given userObjectIdentifier and application.
        /// </summary>
        /// <param name="userObjectIdentifier">The user object identifier.</param>
        /// <param name="applicationName">Name of the application.</param>
        /// <returns>The instance of <see cref="IEnumerable{String}"/>.</returns>
        private IEnumerable<string> GetConnectionIdsInternal(string userObjectIdentifier, string applicationName)
        {
            List<string> connectionIds = new List<string>();
            HashSet<UserConnectionInfo> userConnectionsSet = null;
            lock (this.lockObject)
            {
                if (this.connections.TryGetValue(userObjectIdentifier, out userConnectionsSet))
                {
                    foreach (var userConnectionInfo in userConnectionsSet)
                    {
                        if (userConnectionInfo.ApplicationName.Equals(applicationName, StringComparison.Ordinal)
                            || !userConnectionInfo.ApplicationName.Equals(UserConnectionTracker.BrowserApplication, StringComparison.Ordinal))
                        {
                            connectionIds.Add(userConnectionInfo.ConnectionId);
                        }
                    }
                }
            }

            return connectionIds;
        }

        /// <summary>
        /// Sets the application name internal.
        /// </summary>
        /// <param name="userObjectIdentifier">The user object identifier.</param>
        /// <param name="userConnectionInfo">The instance of <see cref="UserConnectionInfo"/>..</param>
        private void SetApplicationNameInternal(string userObjectIdentifier, UserConnectionInfo userConnectionInfo)
        {
            HashSet<UserConnectionInfo> userConnectionsSet;
            lock (this.lockObject)
            {
                if (this.connections.TryGetValue(userObjectIdentifier, out userConnectionsSet))
                {
                    _ = userConnectionsSet.Remove(userConnectionInfo);
                }
                else
                {
                    userConnectionsSet = new HashSet<UserConnectionInfo>();
                    _ = this.connections.TryAdd(userObjectIdentifier, userConnectionsSet);
                }

                _ = userConnectionsSet.Add(userConnectionInfo);
            }
        }

        /// <summary>
        /// Removes the connection privately.
        /// </summary>
        /// <param name="userObjectIdentifier">The user object identifier.</param>
        /// <param name="connectionId">The connection identifier.</param>
        private void RemoveConnectionInternal(string userObjectIdentifier, string connectionId)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(userObjectIdentifier), "userObjectIdentifier is invalid.");
            Debug.Assert(!string.IsNullOrWhiteSpace(connectionId), "connectionId is invalid.");
            UserConnectionInfo userConnectionInfo = new UserConnectionInfo(connectionId);
            HashSet<UserConnectionInfo> userConnectionsSet;
            lock (this.lockObject)
            {
                if (this.connections.TryGetValue(userObjectIdentifier, out userConnectionsSet) &&
                    userConnectionsSet.Any())
                {
                    if (userConnectionsSet.Count > 1)
                    {
                        _ = userConnectionsSet.Remove(userConnectionInfo);
                    }
                    else
                    {
                        _ = this.connections.TryRemove(userObjectIdentifier, out _);
                    }
                }
            }
        }

        /// <summary>
        /// Adds the connection to the dictionary.
        /// </summary>
        /// <param name="userObjectIdentifier">The user object identifier.</param>
        /// <param name="userConnectionInfo">The instance of <see cref="UserConnectionInfo"/>..</param>
        private void AddConnection(string userObjectIdentifier, UserConnectionInfo userConnectionInfo)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(userObjectIdentifier), "UserObjectIdentifier is null.");
            Debug.Assert(userConnectionInfo != null, "The userConnectionInfo is null.");
            HashSet<UserConnectionInfo> userConnectionsSet;
            lock (this.lockObject)
            {
                if (!this.connections.TryGetValue(userObjectIdentifier, out userConnectionsSet))
                {
                    userConnectionsSet = new HashSet<UserConnectionInfo>();
                    this.connections[userObjectIdentifier] = userConnectionsSet;
                }

                _ = userConnectionsSet.Add(userConnectionInfo);
            }
        }
    }
}
