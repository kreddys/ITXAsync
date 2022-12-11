# ITXAsync
This repository contains an implementation of an Azure Durable Function that coordinates the execution of a series of activities. The function receives a MapRequest object as input and returns a MapStatusResponse object as output.

# Overview
The function uses the IDurableOrchestrationContext interface to define an orchestrator function called RunOrchestrator, which coordinates the execution of several activities defined by other functions. These activities include:

InitiateRequest: This function initiates a request to a specified URI using the HttpClient class.
FetchResult: This function retrieves the result of the request initiated by InitiateRequest.
SendCallback: This function sends a callback to a specified URI using the HttpClient class.

# Usage
To use this function, you will need an Azure account and the Azure Functions runtime. You can then deploy the code to your Azure account and invoke the RunOrchestrator function using the Azure Functions runtime. The RunOrchestrator function expects a MapRequest object as input, which contains information about the map that needs to be executed.
