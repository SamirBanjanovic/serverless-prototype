# Serverless Prototype

This is a prototype of a serverless platform built in C#/.NET using Microsoft Azure and Windows containers. The goals of this implementation are to improve the performance of serverless platforms and explore platform designs while maintaining a simple implementation.

## Implementation Overview

<p align="center">
  <img align="center" src="https://mgarrettm.blob.core.windows.net/research/implementation.png" alt="Implementation Overview" />
</p>

The platform depends upon Azure Storage for data persistence and for its messaging layer. Besides Azure Storage services, the implementation is comprised of two components: a web service which exposes the platform's public REST API, and a worker service which manages and executes function containers. The web service discovers available workers through a messaging layer consisting of various Azure Storage queues. Function metadata is stored in Azure Storage tables, and function code is stored in Azure Storage blobs.

A in-depth discussion of the current platform and its limitations is available <a href='https://mgarrettm.blob.core.windows.net/research/prototype.pdf'>here</a>.

## Performance Measurements

Performance measurements of this prototype and other serverless platform was taken using a <a href='https://github.com/mgarrettm/serverless-performance'>performance tool</a> created to compare serverless offerings. Example performance measurements are available there, and in the paper linked above.

## Getting Started

Coming soon :)