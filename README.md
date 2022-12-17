# ITX Async
This repository contains an implementation of an Azure Durable Function that coordinates the execution of a series of activities which serve as a webhook wrapper for ITX rest api's Asynchronous map execution.

The function can be invoked with a POST request to Uri ```http://localhost:7054/api/ItxAsync_HttpStart``` and body ```MapRequest```

```json
{
        "itxUri": "http://localhost:5214/itx-rs/v1/maps/direct/",
        "callBackUri": "https://test.azure.com/callback/",
        "map": {
            "name": "ADF_EMPLOYEES_JSON2XML",
            "audit": true,
            "trace": false,
            "waitSeconds": 10,
            "maxRetries": 5,
            "inputs": [
                {
                    "cardNumber": 1,
                    "source": "FILE",
                    "file": "/data/workingDir/bcbad2a1-6999-408a-83ae-865b4ee18a07/5c2a12dc-47a4-4979-a91c-3a0813952b59/employees.json"
                }
            ],
            "outputs": [
                {
                    "cardNumber": 2,
                    "source": "FILE",
                    "file": "/data/workingDir/bcbad2a1-6999-408a-83ae-865b4ee18a07/5c2a12dc-47a4-4979-a91c-3a0813952b59/out2"
                }
            ]
        }
    }
```

# Overview
The function uses the IDurableOrchestrationContext interface to define an orchestrator function called RunOrchestrator, which coordinates the execution of several activities defined by other functions. These activities include:

## InitiateRequest
This function initiates a POST request to Itx Rest Api Url ```http://localhost:5214/itx-rs/v1/maps/direct/ADF_EMPLOYEES_JSON2XML?&input=1;FILE;/data/workingDir/bcbad2a1-6999-408a-83ae-865b4ee18a07/5c2a12dc-47a4-4979-a91c-3a0813952b59/employees.json&output=2;FILE;/data/workingDir/bcbad2a1-6999-408a-83ae-865b4ee18a07/5c2a12dc-47a4-4979-a91c-3a0813952b59/out2&return=audit```

This function returns statusUrl from location header

## FetchResult
This function retrieves the result of the request initiated by InitiateRequest by using the statusUrl

The function waits for ```10``` seconds before making the first GET call to statusUrl and it retries for maximum of ```5``` times

## SendCallback
This function sends a HTTP POST callback to a specified ```callBackUri```. 

### Success

```json
{
        "Output": {
            "status": 0,
            "start_timestamp": "2022-12-11T10:27:36.477+0000",
            "elapsed_time": 10011,
            "status_message": "Map completed successfully",
            "audit_href": "http://localhost:5214/itx-rs/v1/maps/direct/5b333424-5b71-4461-960d-2cd097cd2ce9/audit"
        },
        "Error": null,
        "StatusCode": "0"
    }
```

### Failure

```json
{
        "Output": {
            "status": 12,
            "outputs": null,
            "start_timestamp": "2022-12-17T18:38:09.024+0000",
            "elapsed_time": 1,
            "status_message": "Source not available",
            "audit_href": "http://localhost:5214/itx-rs/v1/maps/direct/7e55ec7a-146a-4b96-b798-328e65a0ec12/audit",
            "trace_href": null
        },
        "Error": {
            "ErrorCode": "12",
            "Message": "Source not available"
        },
        "StatusCode": "512"
    }
```

### Timeout

```json
{
        "Output": {
            "status": 408,
            "outputs": null,
            "start_timestamp": null,
            "elapsed_time": 0,
            "status_message": "Map Not Completed Within Timeout",
            "audit_href": null,
            "trace_href": null
        },
        "Error": {
            "ErrorCode": "408",
            "Message": "Map Not Completed Within Timeout"
        },
        "StatusCode": "908"
    }
```

# Usage
To use this function, you will need an Azure account and the Azure Functions runtime. You can then deploy the code to your Azure account and invoke the RunOrchestrator function using the Azure Functions runtime. The RunOrchestrator function expects a MapRequest object as input, which contains information about the map that needs to be executed.
