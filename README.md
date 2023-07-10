# K-Tester
K-Tester is a simple tool to get, verify & send kafka messages inside a topic.

## Why this library?
Sometime exclude the kafka as a problem in a bug of an application can be very usefull for debugging.

This tool permit to know quickly is a message is currently present and don't put any problem to the app. Or send a message to verify the behavior of the app.

## Functions
You can:
- get messages for a topic
- copy a message
- send messages
- filter messages
- save multiple configuration
- import/export configuration
- list & select topics of the brokers

More are about to come. Don't hesitate to propose.
Plan for the future:
- Specify the groupid (verify if all messages of the group has been retreived)
- Clean the messages for a group
- Choose display ordering
- Improve performance for heavy messages

## More to know
K-Tester is made with dotnet [BlazorServer](https://docs.microsoft.com/en-us/aspnet/core/blazor) technology.

About data configurations, no database. All data are saved inside the local storage of your browser. If you change your browser, you can export & import the configurations.

By default, groupid used to connect is a generated `GUID`. This one is refreshed each time a new query is asked.

# Contributing
See [CONTRIBUTING.md](CONTRIBUTING.md) for detailed information about how to contribute to the project.

# Launch it
## 1 - Docker
The project is pushed in docker hub. On a machine with docker installed you can run the command:
```
docker run -p 5000:80 vptech/k-tester
```
and go to [http://localhost:5000](http://localhost:5000) on your favorite browser.

## 2 - Dotnet
Clone the repository, run the project with (you need to have [dotnet 7.0 sdk](https://dotnet.microsoft.com/download/dotnet) installed):
```
dotnet run
```
and go to [http://localhost:5000](http://localhost:5000) on your favorite browser.

# LICENSE
This project is licensed under the **[ISC LICENSE](LICENSE)**.