# Azure Webjobs SDK Extension for Azure Queue

## Introduction

This extension will enable you to trigger funcions and receive the group of messages instead of a single message like with `[QueueTrigger]`

Once referenced you can enable it on the `JobHostConfiguration` object.

    var config = new JobHostConfiguration
    {
        StorageConnectionString = "...",
        DashboardConnectionString = "..."
    };
    config.UseGroupQueueTriggers();
    var host = new JobHost(config);
    host.RunAndBlock();
    
It supports **POCO** objects and returns always a **List**.

And decorate each function like this:

`public static void MyFunction([GroupQueueTrigger("my_queue")]List<MyClass> messages)`

This way you will only get **one trigger** of the function and receive all the obtained messages, instead of one trigger per message with the `[QueueTrigger]`.

There are optional parameters too: the **size** of the requested block:

`public static void MyFunction([GroupQueueTrigger("my_queue", 10)]List<MyClass> messages)`

And the Minimun and Maximun Polling **intervals** (in minutes):

`public static void MyFunction([GroupQueueTrigger("my_queue", 10, 1, 10)]List<MyClass> messages)`


## Installation

You can obtain it [through Nuget](https://www.nuget.org/packages/Microsoft.Azure.WebJobs.Extensions.GroupQueueTrigger/) with:

    Install-Package Microsoft.Azure.WebJobs.Extensions.GroupQueueTrigger

Or **clone** this repo and reference it.
