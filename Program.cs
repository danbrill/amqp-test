using System.CommandLine;
using Amqp;

const string securedScheme = "AMQPS";
const string unsecuredScheme = "AMQP";
const int securedPort = 5671;
const int unsecuredPort = 5672;

var secureOption = new Option<bool>("--secure", () => false, $"Indicates whether to attempt an unsecured (False/missing = {unsecuredScheme}) or a secured (True = {securedScheme}) connection");
var hostOption = new Option<string>("--host", "The broker host name") {IsRequired = true};
var portOption = new Option<int?>("--port", $"The broker host port (if not specified, {unsecuredPort} will be assumed for {unsecuredScheme}, {securedPort} for {securedScheme})");
var userOption = new Option<string?>("--user", "The broker user's name");
var passwordOption = new Option<string?>("--password", "The broker user's password");
var checkCertificateRevocationOption = new Option<bool>("--checkCertificateRevocation", () => false, "Indicates whether to check for certificate revocation or not.");
var bypassRemoveCertificateValidationOption = new Option<bool>("--bypassRemoveCertificateValidation", () => false, "Indicates whether to bypass remote certificate validition or not.");
var queueNameOption = new Option<string>("--queueName", () => "queue1", "The name of the queue/topic");
var senderNameOption = new Option<string>("--senderName", () => "sender1", "The sender's name");
var receiverNameOption = new Option<string>("--receiverName", () => "receiver1", "The receiver's name");
var messageBodyOption = new Option<string>("--messageBody", () => "Hello World!", "The message's body");
var sendCountOption = new Option<int>("--sendCount", () => 1, "The number of identical messages to send");
var receiveCountOption = new Option<int>("--receiveCount", () => 1, "The number of messages to be received before exiting");
var receiveTimeoutSecondsOption = new Option<int>("--receiveTimeoutSeconds", () => -1, "The number of seconds to wait for a message to be available (-1 = wait forever)");

var sendCommand = new Command("send", "AMQP message sender")
{
    secureOption, hostOption, portOption, userOption, passwordOption, checkCertificateRevocationOption, bypassRemoveCertificateValidationOption, queueNameOption,
    senderNameOption, messageBodyOption, sendCountOption
};

var receiveCommand = new Command("receive", "AMQP message receiver")
{
    secureOption, hostOption, portOption, userOption, passwordOption, checkCertificateRevocationOption, bypassRemoveCertificateValidationOption, queueNameOption,
    receiverNameOption, receiveCountOption, receiveTimeoutSecondsOption
};

var rootCommand = new RootCommand("AMQP test tool") {sendCommand, receiveCommand};

sendCommand.SetHandler(
    (Func<bool, string, int?, string?, string?, bool, bool, string, string, string, int, Task>)SendCommandHandler, 
    secureOption, hostOption, portOption, userOption, passwordOption, checkCertificateRevocationOption, bypassRemoveCertificateValidationOption, queueNameOption, senderNameOption, messageBodyOption, sendCountOption
);

receiveCommand.SetHandler(
    (Func<bool, string, int?, string?, string?, bool, bool, string, string, int, int, Task>)ReceiveCommandHandler, 
    secureOption, hostOption, portOption, userOption, passwordOption, checkCertificateRevocationOption, bypassRemoveCertificateValidationOption, queueNameOption, receiverNameOption, receiveCountOption, receiveTimeoutSecondsOption
);

return rootCommand.Invoke(args);

static async Task SharedCommandHandler(bool secure, string host, int? port, string? user, string? password, bool checkCertificateRevocation, bool bypassRemoveCertificateValidation, Func<Session, Task> performAction)
{
    Connection? connection = null;
    Session? session = null;

    try
    {
        var address = new Address(host, port ?? (secure ? securedPort : unsecuredPort), user, password, "/", (secure ? securedScheme : unsecuredScheme).ToLower());
        Console.WriteLine($"Connecting to {address.Scheme}://{address.User}:{new string('*', address.Password.Length)}@{address.Host}:{address.Port}{address.Path}");

        var factory = new ConnectionFactory
        {
            SSL = {CheckCertificateRevocation = checkCertificateRevocation}
        };

        factory.SSL.RemoteCertificateValidationCallback += (sender, certificate, chain, errors) =>
        {
            var valid = true;

            // ReSharper disable once InvertIf
            if (!bypassRemoveCertificateValidation)
            {
                // ReSharper disable once InvertIf
                if (chain is not null && chain.ChainStatus.Any())
                {
                    Console.WriteLine(string.Join("\n", chain.ChainStatus.Select(_ => _.StatusInformation)));
                    valid = false;
                }
            }

            return valid;
        };

        connection = await factory.CreateAsync(address).ConfigureAwait(false);
        session = new Session(connection);
        await performAction(session).ConfigureAwait(false);
    }
    catch (Exception ex) { Console.WriteLine($"Error: {ex.Message}"); }
    finally
    {
        if (session is not null) {await session.CloseAsync().ConfigureAwait(false);}
        if (connection is not null) {await connection.CloseAsync().ConfigureAwait(false);}
    }
}

static async Task SendCommandHandler(bool secure, string host, int? port, string? user, string? password, bool checkCertificateRevocation, bool bypassRemoteCertificateValidition, string queueName, string senderName, string messageBody, int sendCount)
{
    await SharedCommandHandler(secure, host, port, user, password, checkCertificateRevocation, bypassRemoteCertificateValidition, async session =>
    {
        SenderLink? sender = null;

        try
        {
            sender = new SenderLink(session, senderName, queueName);
            using var message = new Message(messageBody);

            for (var i = 0; i < sendCount; i++)
            {
                await sender.SendAsync(message).ConfigureAwait(false);
                Console.WriteLine($"Message {i + 1} sent");
            }
        }
        catch (Exception ex) {Console.WriteLine($"Error: {ex.Message}");}
        finally
        {
            if (sender is not null) {await sender.CloseAsync().ConfigureAwait(false);}
        }
    })
    .ConfigureAwait(false);
}

static async Task ReceiveCommandHandler(bool secure, string host, int? port, string? user, string? password, bool checkCertificateRevocation, bool bypassRemoteCertificateValidition, string queueName, string receiverName, int receiveCount, int receiveTimeoutSeconds)
{
    await SharedCommandHandler(secure, host, port, user, password, checkCertificateRevocation, bypassRemoteCertificateValidition, async session =>
    {
        ReceiverLink? receiver = null;

        try
        {
            receiver = new ReceiverLink(session, receiverName, queueName);

            for (var i = 0; i < receiveCount; i++)
            {
                using var message = await receiver.ReceiveAsync(receiveTimeoutSeconds == -1 ? Timeout.InfiniteTimeSpan : TimeSpan.FromSeconds(receiveTimeoutSeconds)).ConfigureAwait(false);
                receiver.Accept(message);
                Console.WriteLine($"Message {i + 1} received");
            }
        }
        catch (Exception ex) {Console.WriteLine($"Error: {ex.Message}");}
        finally
        {
            if (receiver is not null) {await receiver.CloseAsync().ConfigureAwait(false);}
        }
    })
    .ConfigureAwait(false);
}