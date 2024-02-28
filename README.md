# GR8Tech.TestUtils.SignalRClient

Author: Mykola Panasiuk

## TL;DR
This repository contains primary the source code for building NuGet package to Open Source:
- **GR8Tech.TestUtils.SignalRClient**

This client is developed to communicate with HubServer based on **Microsoft.AspNetCore.SingleR.Client** through WebSockets. It contains such interfaces:
- `SignalRClientFactory : ISignalRClientFactory`
- `HubClient : IHubClient`
- `Subscription<T> : ISubscription<T>`

### Key features

- Serilog as a logger with Context for each class
- Ability to read date from a settings json file
- Support cascade settings
- Ability to create HubClient for WebSocket communication with HubServer
- Ability to create Subscriptions for HubClient
- Configurable internal buffer
- Ability to filter message after consume and saving to the internal buffer
- Wait for messages to appear with condition
- Wait for no message

## Settings file

Settings file might be named either `test-settings.json` or `appsettings.json`.

### The simplest example

```json
{
  "SignalRSettings": {
    "Connections": {
      "local": {
        "Url": "http://localhost:5887/hello"
      }
    }
  }
}
```

### Full example
```json
{
  "SignalRSettings": {
    "DefaultConnectionConfig": {
      "BufferSize": 5000,
      "WaiterSettings": {
        "RetryCount": 30,
        "Interval": "00:00:01"
      },
      "MessageProtocol": "Json",
      "ServerTimeout": "00:01:00",
      "HandshakeTimeout": "00:00:10",
      "KeepAliveInterval": "00:00:10"
    },
    "Connections": {
      "local": {
        "Url": "http://localhost:5887/hello",
        "MessageProtocol": "Json",
        "ServerTimeout": "00:01:00",
        "HandshakeTimeout": "00:00:10",
        "KeepAliveInterval": "00:00:10",
        "BufferSize": 5000,
        "WaiterSettings": {
          "RetryCount": 30,
          "Interval": "00:00:01"
        }
      },
      "stage": {
        "Url": "http://stage:5887/hello"
      }
    }
  }
}
```

Thanks to Nullable types you do NOT need to specify each line of settings, all of them are handled implicitly with default values.

## Code examples
All examples below will be shown for next settings json file:

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Debug",
      "Override": {
        "GR8Tech.TestUtils.SignalRClient.Implementation": "Information"
      }
    }
  },
  "SignalRSettings": {
    "DefaultConnectionConfig": {
      "BufferSize": 5000,
      "WaiterSettings": {
        "RetryCount": 6,
        "Interval": "00:00:01"
      },
      "MessageProtocol": "Json",
      "ServerTimeout": "00:01:00",
      "HandshakeTimeout": "00:00:10",
      "KeepAliveInterval": "00:00:10"
    },
    "Connections": {
      "local": {
        "Url": "http://localhost:5026/testServiceHub",
        "MessageProtocol": "Json",
        "ServerTimeout": "00:01:00",
        "HandshakeTimeout": "00:00:10",
        "KeepAliveInterval": "00:00:10",
        "BufferSize": 5000,
        "WaiterSettings": {
          "RetryCount": 6,
          "Interval": "00:00:01"
        }
      }
    }
  }
}
```

### KafkaAdminClient examples

```c#
// Create simple hub Client with connection settings from json file
var hubClient = _signalRClientFactory.CreateClient();

// Connect HubClient to HubServer
await hubClient.ConnectAsync();

// Create new Subscription for HubClient
var subName = "subName";
var methodName = "methodName";
var subscription = hubClient.AddSubscription<CurrencyRate>(subName, methodName, []);

// Activate the Subscription
subscription.Subscribe();

// Add filter to the Subscription 
subscription.AddFilter("my-filter", x => x.Message.Key == keyToSearch);

// Remove filter from the Subscription
consumer.RemoveFilter("my-filter");

// Wait and get message 
var result = await subscription.WaitAndGetMessage(x => x.Message.Key == key);

// Wait and get messages 
var result = await subscription.WaitAndGetMessages(
    x => x.Message.Key == key1 || x.Message.Key == key2);

// Wait and get messages with condition
var result = await subscription.WaitAndGetMessages(
    x => x.Message.Key == key1 || x.Message.Key == key2, 
    r => r.Count == 2);

// Remove saved masseges from internal buffer
subscription.RemoveMessages(x => x.Message.Key == key);
```
