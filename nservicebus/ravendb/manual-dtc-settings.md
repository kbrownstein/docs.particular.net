---
title: 'Setting RavenDB DTC settings manually'
summary: 'Guidance on how to change the RavenDB ResourceManagerID and TransactionRecoveryStorage'
tags: [RavenDB, Persistence]
redirects:
 - nservicebus/ravendb/how-to-change-resourcemanagerid
 - nservicebus/ravendb/resourcemanagerid
---

In order to provide the most reliable support for distributed transactions in RavenDB, a custom DocumentStore must be provided to NServiceBus and configured to uniquely identify the NServiceBus endpoint to the Distributed Transaction Coordinator (DTC) and provide a storage location for uncommitted transaction recovery.

## Definitions

Support for the Distributed Transaction Coordinator (DTC) in RavenDB is dependent upon the **ResourceManagerId** and **TransactionRecoveryStorage** settings.

### ResourceManagerId

The ResourceMangaerId is a Guid that uniquely identifies a transactional resource on a machine, and must be both unique on that machine, and stable (deterministic) between restarts of the process utilizing the DTC. If more than one RavenDB document store attempts to use the same ResourceManagerId, it can result in the following error during a commit operation:

> "A resource manager with the same identifier is already registered with the specified transaction coordinator"

### TransactionRecoveryStorage

To guard against the loss of a committed transaction, RavenDB requires a storage location to persist data in the event of a process crash immediately following a transaction commit.

RavenDB provides the following methods for persisting transaction recovery storage information:

* `VolatileTransactionRecoveryStorage` persists the information in memory, and since the information will be lost during a process restart, is not a safe storage method.
* `IsolatedStorageTransactionRecoveryStorage` persists the information in [Isolated Storage](https://msdn.microsoft.com/en-us/library/system.io.isolatedstorage.aspx). This is the default for NServiceBus.RavenDB when connecting with a connection string, as it does not require any additional configuration. However, in high-contention scenarios, IsolatedStorage has been found to become unstable and may begin to throw exceptions. Therefore using LocalDirectoryTransactionRecoveryStorage is preferable.
* `LocalDirectoryTransactionRecoveryStorage` stores transaction recovery information to a local path on disk, and is the safest option for production scenarios, but must be configured with a directory that is writeable by the application.

## Configuring safe settings

In order to configure safe settings for production use of RavenDB, construct a `DocumentStore` instance and configure the `ResourceManagerId` and `TransactionRecoveryStorage` properties as shown in the following code:

snippet:RavenDBManualDtcSettingExample

In order to provide transaction safety, the following must be observed:

* `ResourceManagerId` must be constant across process restarts, and uniquely identify the process running on the machine. (Do not use `Guid.NewGuid()`.) Otherwise, the transaction recovery process will fail when the process restarts.
* `TransactionRecoveryStorage` must be set to an instance of `LocalDirectoryTransactionRecoveryStorage`, configured to a directory that is constant across process restarts, and writeable by the process.

## Configuration by convention

It can be cumbersome to manage these settings for multiple endpoints, so it is preferable to create a convention that will calculate a unique ResourceManagerId,
and then use this value to create a storage location for TransactionRecoveryStorage as well. 

snippet:RavenDBDtcSettingsByConvention

It is important to keep a few things in mind when determining a convention.

The string provided to DeterministicGuidBuilder will define the ResourceManagerId, and thus the identity of the endpoint. This string value must then be unique within the scope of the server. The EndpointName or LocalAddress provide attractive options as they are normally unique per server.

An exception is side-by-side deployment, where an old version and new version of the same endpoint run concurrently, processing messages from the same queue, in order to vet the new version and enable easy rollback to the previous version. In this case using EndpointName or LocalAddress would result in duplicate ResourceManagerId values on the same server, which would lead to DTC exceptions. In this case, the a versioning scheme must be created, and the version included in the string provided to DeterministicGuidBuilder.

The exact convention used must be appropriately defined to match the deployment strategy in use for the endpoint.

## Safely decommissioning endpoints

If an endpoint terminates unexpectedly for any reason, data can be left behind in the transaction recovery storage which represents transactions not yet committed to the RavenDB database, but which may be recovered when the endpoint restarts.

In order to avoid losing data, it is important to ensure that endpoints that are decommissioned are taken offline through graceful endpoint shutdowns, and then the transaction recovery storage directory should be inspected to ensure no recovery files remain behind.
