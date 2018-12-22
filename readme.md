This is a tool for scaling azure sql databases.

Run from CLI:

`rescaler [-simulate] [-verbose] <edition> <size> [+includedb1] [-excludedb1] [+includedb2] [-excludedb2] ...`

Where edition is Basic/Standard/Premium, and size is Basic/S0.../P1...

Actual names of databases are specified using + and -, will be matches as substring of resource group, server and database.

Credentials are specified using a service principal by setting the environment variables:
`AzureTenantId`, `AzureClientId` and `AzureClientSecret`.

Multiple service principals can be specified by prefixing/postfixing the environment variables.

Keep in mind that azure sql databases will typical go offline 30s-2min while they are rescaled.