using System.CommandLine;
using Amqp;

const string secureScheme = "AMQPS";
const string insecureScheme = "AMQP";
const int securePort = 5671;
const int insecurePort = 5672;

var secureOption = new Option<bool>("--secure", () => false, $"Indicates whether to attempt an insecure (False/missing = {insecureScheme}) or a secure (True = {secureScheme}) connection");
var hostOption = new Option<string>("--host", "The broker host name") {IsRequired = true};
var portOption = new Option<int?>("--port", $"The broker host port (if not specified, {insecurePort} will be assumed for {insecureScheme}, {securePort} for {secureScheme})");
var userOption = new Option<string?>("--user", "The broker user's name");
var passwordOption = new Option<string?>("--password", "The broker user's password");
var queueNameOption = new Option<string>("--queueName", () => "queue1", "The name of the queue/topic");
var senderNameOption = new Option<string>("--senderName", () => "sender1", "The sender's name");
var receiverNameOption = new Option<string>("--receiverName", () => "receiver1", "The receiver's name");
var messageBodyOption = new Option<string>("--messageBody", () => "Hello World!", "The message's body");
var sendCountOption = new Option<int>("--sendCount", () => 1, "The number of identical messages to send");
var receiveCountOption = new Option<int>("--receiveCount", () => 1, "The number of messages to be received before exiting");
var receiveTimeoutSecondsOption = new Option<int>("--receiveTimeoutSeconds", () => -1, "The number of seconds to wait for a message to be available (-1 = wait forever)");

var sendCommand = new Command("send", "AMQP message sender")
{
    secureOption,
    hostOption,
    portOption,
    userOption,
    passwordOption,
    queueNameOption,
    senderNameOption,
    messageBodyOption,
    sendCountOption
};

var receiveCommand = new Command("receive", "AMQP message receiver")
{
    secureOption,
    hostOption,
    portOption,
    userOption,
    passwordOption,
    queueNameOption,
    receiverNameOption,
    receiveCountOption,
    receiveTimeoutSecondsOption
};

var rootCommand = new RootCommand("AMQP test tool")
{
    sendCommand,
    receiveCommand
};

sendCommand.SetHandler((bool secure, string host, int? port, string? user, string? password, string queueName, string senderName, string messageBody, int sendCount) =>
{
    Connection? connection = null;
    Session? session = null;
    SenderLink? sender = null;

    try
    {
        var address = new Address(host, port ?? (secure ? securePort : insecurePort), user, password, "/", (secure ? secureScheme : insecureScheme).ToLower());
        Console.WriteLine($"Connecting to {address.Scheme}://{address.User}:{new string('*', address.Password.Length)}@{address.Host}:{address.Port}{address.Path}");
        
        connection = new Connection(address);
        session = new Session(connection);
        sender = new SenderLink(session, senderName, queueName);

        using var message = new Message(messageBody);

        for (var i = 0; i < sendCount; i++)
        {
            sender.Send(message);
            Console.WriteLine($"Message {i + 1} sent");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
    }
    finally
    {
        sender?.Close();
        session?.Close();
        connection?.Close();
    }
}, secureOption, hostOption, portOption, userOption, passwordOption, queueNameOption, senderNameOption, messageBodyOption, sendCountOption);

receiveCommand.SetHandler((bool secure, string host, int? port, string? user, string? password, string queueName, string receiverName, int receiveCount, int receiveTimeoutSeconds) =>
{
    Connection? connection = null;
    Session? session = null;
    ReceiverLink? receiver = null;

    try
    {
        var address = new Address(host, port ?? (secure ? securePort : insecurePort), user, password, "/", (secure ? secureScheme : insecureScheme).ToLower());
        Console.WriteLine($"Connecting to {address.Scheme}://{address.User}:{new string('*', address.Password.Length)}@{address.Host}:{address.Port}{address.Path}");

        connection = new Connection(address);
        session = new Session(connection);
        receiver = new ReceiverLink(session, receiverName, queueName);

        for (var i = 0; i < receiveCount; i++)
        {
            using var message = receiver.Receive(receiveTimeoutSeconds == -1 ? Timeout.InfiniteTimeSpan : TimeSpan.FromSeconds(receiveTimeoutSeconds));
            Console.WriteLine($"Message {i + 1} received");
            receiver.Accept(message);
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
    }
    finally
    {
        receiver?.Close();
        session?.Close();
        connection?.Close();
    }
}, secureOption, hostOption, portOption, userOption, passwordOption, queueNameOption, receiverNameOption, receiveCountOption, receiveTimeoutSecondsOption);

return rootCommand.Invoke(args);
