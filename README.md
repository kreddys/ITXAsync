# ITX Async
This repository contains an implementation of an Azure Durable Function that coordinates the execution of a series of activities which serve as a webhook wrapper for ITX rest api's Asynchronous map execution.

The function can be invoked with a POST request to Uri ```http://localhost:7054/api/ItxAsync_HttpStart``` and body ```MapRequest```

```json
    {
        "itxUri": "http://localhost:5214/itx-rs/v1/maps/direct/",
        "callBackUri": "https://test.azure.com/callback/",
        "frameworkMap": {
            "name": "ADF_GEN_FMWK",
            "audit": true,
            "trace": false,
            "inputCard": 1,
            "outputCard": 2
        },
        "runMap": {
            "name": "ADF_EMPLOYEES_JSON2XML",
            "audit": true,
            "trace": true,
            "waitSeconds": 10,
            "maxRetries": 5,
            "inputs": [
                {
                    "cardNumber": 1,
                    "source": "FILE",
                    "file": "/data/tmp/ADF_EMPLOYEES_JSON2XML/employees.json"
                }
            ],
            "outputs": [
                {
                    "cardNumber": 2,
                    "source": "FILE",
                    "file": "/data/tmp/ADF_EMPLOYEES_JSON2XML/employees2.xml"
                }
            ]
        }
    }
```

# Overview
The function uses the IDurableOrchestrationContext interface to define an orchestrator function called RunOrchestrator, which coordinates the execution of several activities defined by other functions. These activities include:

## InitiateRequest
This function initiates a POST request to Itx Rest Api Url ```http://localhost:5214/itx-rs/v1/maps/direct/ADF_GEN_FMWK?input=1&output=2&return=audit``` and send the data in ```runMap``` object as input card 1

The map ```ADF_GEN_FMWK``` invokes another map ```ADF_EMPLOYEES_JSON2XML``` using RUN() function after overriding ```inputs``` and ```outputs```

This function returns statusUrl from location header

## FetchResult
This function retrieves the result of the request initiated by InitiateRequest by using the statusUrl

The function waits for ```10``` seconds before making the first GET call to statusUrl and it retries for maximum of ```5``` times

## SendCallback
This function sends a HTTP POST callback to a specified ```itxUri```. 

### Success

```json
{
        "Output": {
            "status": 0,
            "outputs": [
                {
                    "href": "http://localhost:5214/itx-rs/v1/maps/direct/5b333424-5b71-4461-960d-2cd097cd2ce9/outputs/2",
                    "card_number": 2,
                    "mime_type": "application/octet-stream"
                }
            ],
            "start_timestamp": "2022-12-11T10:27:36.477+0000",
            "elapsed_time": 10011,
            "status_message": "Map completed successfully",
            "audit_href": "http://localhost:5214/itx-rs/v1/maps/direct/5b333424-5b71-4461-960d-2cd097cd2ce9/audit",
            "trace_href": null
        },
        "Error": null,
        "StatusCode": "0"
    }
```

### Failure

```json
{
        "Output": {
            "status": 30,
            "outputs": [
                {
                    "href": "http://localhost:5214/itx-rs/v1/maps/direct/8c90bf53-b02f-4222-a5b8-045289bea590/outputs/2",
                    "card_number": 2,
                    "mime_type": "application/octet-stream"
                }
            ],
            "start_timestamp": "2022-12-11T10:29:59.567+0000",
            "elapsed_time": 1,
            "status_message": "FAIL function aborted map",
            "audit_href": "http://localhost:5214/itx-rs/v1/maps/direct/8c90bf53-b02f-4222-a5b8-045289bea590/audit",
            "trace_href": null
        },
        "Error": {
            "ErrorCode": "30",
            "Message": "FAIL function aborted map"
        },
        "StatusCode": "430"
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
        "StatusCode": "808"
    }
```

# Usage
To use this function, you will need an Azure account and the Azure Functions runtime. You can then deploy the code to your Azure account and invoke the RunOrchestrator function using the Azure Functions runtime. The RunOrchestrator function expects a MapRequest object as input, which contains information about the map that needs to be executed.
