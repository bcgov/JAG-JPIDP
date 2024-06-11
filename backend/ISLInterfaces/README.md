# ISL Interfaces

## This service provides API(s) that are available to secured ISL services.

Currently this supports getting the list of users for a given RCC.
This has been separated out to a new service as this is called very frequently and was deemed to be a candidate for a new service that can scale as necessary.

The service requires a read-only db account with access to SubmittingAgencyRequests and Party table only. No other tables should be 
accessible unless needed by future services.

