
# Go to maintenance mode

## Make sure diam-prod-maintenance deployment has up to 1 instance running

Wait for instance to be ready

## Change Ingress
```
From the Command Line:
login to Gold
oc project e27db1-prod
oc apply -f maintenance-ingress.yaml
```

## Verify Site

[jpidp.justice.gov.bc.ca](https://jpidp.justice.gov.bc.ca/)
[accused.bcprosecution.gov.bc.ca](https://accused.bcprosecution.gov.bc.ca/)
[agencies.justice.gov.bc.ca](https://agencies.justice.gov.bc.ca/)
[access.bcps.gov.bc.ca](https://access.bcps.gov.bc.ca/)
[legalcounsel.justice.gov.bc.ca](https://legalcounsel.justice.gov.bc.ca/)

# Leave maintenance mode

Verify new site is up and running.

# Change Route

```
oc project e27db1-prod
oc apply -f regular-ingress.yaml
```

## Scale diam-prod-maintenance down to 0 instances to save resources
