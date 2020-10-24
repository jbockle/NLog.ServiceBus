# NLog.ServiceBus

.NET Core custom NLog target to send log events to an Azure Service Bus Queue or Topic with full support for logging contexts (see https://github.com/NLog/NLog/wiki/Context)

## Installation

Add the nuget package `NLog.ServiceBus` to your project  

dotnet cli: `dotnet add package NLog.ServiceBus`

## Targets

`NLog.ServiceBus` adds two targets.  The options for the targets are identical

- `ServiceBusTopic` - Sends log events to a service bus topic
- `ServiceBusQueue` - Sends log events to a service bus queue

### Target Options

Besides the below options, all standard target options are available, including layout in which you specify any layout type (See https://nlog-project.org/config/?tab=layouts).

- `connectionString`<sup>`required`</sup>  
  the service bus connection string
- `entityPath`<sup>`required`</sup>  
  the topic's or queue's path
- `contentType`<sup>`optional`</sup>  
  the message's content type
- `message-property`<sup>`array` `optional`</sup>  
  set message properties using name/layout
    - `name` the name of the message property i.e. for `Message.MessageId` specify `MessageId`.  This is not a layout, a hardcoded value is required.
    - `layout` the value to set on the message property
- `user-property`<sup>`array` `optional`</sup>  
  set a user (aka custom) property on the message within it's `Message.UserProperties` dictionary
    - `name` the name of the user property.  This is not a layout, a hardcoded value is required.
    - `layout` the value to set on the message property
- `batch-size`<sup>`optional` `[default=1]`</sup>  
  the maximum number of log events to be sent as a batch

## Usage

### From an `nlog.config`

1. add the extension assembly by name to your nlog config  
    i.e.

    ```xml
    <?xml version="1.0" encoding="utf-8" ?>
    <nlog xmlns="http://www.nlog-project.org/schemas/NLog.xsd"
            xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">

        <extensions>
            <!-- add this -->
            <add assembly="NLog.ServiceBus"/>
        </extensions>

        <!-- ..rest.. -->
    </nlog>
    ```
1. setup a target and a rule
    > you can get the connection string from `IConfiguration` if you are using NLog.Web.AspNetCore or NLog.Extensions.Logging by using the `${configsetting:item=configkey}` layout renderer. See https://github.com/NLog/NLog/wiki/ConfigSetting-Layout-Renderer
    ```xml
    <?xml version="1.0" encoding="utf-8" ?>
    <nlog xmlns="http://www.nlog-project.org/schemas/NLog.xsd"
            xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
    
        <extensions>
            <add assembly="NLog.ServiceBus"/>
        </extensions>
    
        <targets>
            <target name="FooTopic" 
                    xsi:type="ServiceBusTopic" 
                    layout="${message}"
                    connectionString="${configsetting:Item=ServiceBus.ConnectionString}" 
                    entityPath="${configsetting:Item=ServiceBus.FooTopicPath" 
                    batchSize="10">
                <message-property name="CorrelationId" layout="${mdlc:requestId}" />
                <message-property name="Label" layout="${logger}" />
                <user-property name="level" layout="${level}" />
            </target>
        </targets>
    
        <rules>
            <logger name="*" minLevel="Debug" writeTo="FooTopic" />
        </rules>
    </nlog>
    ```
    
    > note on rules: you can specify filters using standard nlog configuration filtering syntax see https://github.com/NLog/NLog/wiki/Filtering-log-messages

