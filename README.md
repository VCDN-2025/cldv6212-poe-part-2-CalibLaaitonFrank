[![Review Assignment Due Date](https://classroom.github.com/assets/deadline-readme-button-22041afd0340ce965d47ae6ef1cefeee28c7c493a6346c4f15d667ab976d596c.svg)](https://classroom.github.com/a/sbrQILlA)

Project Overview

This project represents Part 2 of the Portfolio of Evidence (POE) and focuses on integrating additional Azure services into a web application to improve scalability, cost-effectiveness, and cloud suitability. It builds upon the work completed in Part 1, expanding the architecture to include Azure Functions and various cloud storage solutions.

The primary objective of this part is to demonstrate how serverless computing and cloud integration can be used to develop a robust and flexible application infrastructure. The solution leverages Microsoft Azure’s storage and messaging services to automate backend processes, store data efficiently, and ensure smooth system communication.

Objectives

Integrate Azure Functions to handle key backend operations.

Store and manage data using Azure Table Storage, Blob Storage, Queue Storage, and File Shares.

Implement asynchronous data processing and storage management.

Deploy a working Azure Functions app to Microsoft Azure.

Discuss how Azure Event Hubs and Azure Service Bus could enhance customer experience.

Technologies Used
Category	Technology
Cloud Platform	Microsoft Azure
Backend Framework	Azure Functions (.NET C#)
Storage Services	Azure Table Storage, Azure Blob Storage, Azure Queue Storage, Azure File Shares
Data Format	JSON
Tools	Visual Studio 2022, Azure Storage Explorer
Version Control	Git and GitHub
Deployment	Azure Portal
Documentation	Microsoft Word and Markdown
System Architecture

The system follows a serverless and event-driven architecture. Each Azure Function performs a specific role within the cloud ecosystem. The use of multiple Azure services ensures scalability, reliability, and efficient data handling.

Overview of Functions
Function Name	Description
StoreToTableFunction	Stores structured information such as user or transaction data into Azure Table Storage.
WriteToBlobFunction	Uploads and stores files or serialized objects in Azure Blob Storage for large-scale data retention.
QueueTransactionFunction	Sends and retrieves messages from Azure Queue Storage for asynchronous order processing.
WriteToFileShareFunction	Writes processed data to Azure File Shares for shared access across different services.

Each function is independent and designed to be triggered by an HTTP request or a queue event, following the principles of modularity and scalability.
