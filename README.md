# Justin-User-Manager

This service provides access to Justin User information, this permits visbility into the Justin user details.

## Pipeline

This is triggered to build and deploy using the jum-cicd-pipeline.
A push to the develop, test  branches will trigger a deployment. A PR merge into main will (later) trigger a deployment to production - should be setup to use blue-green strategy.