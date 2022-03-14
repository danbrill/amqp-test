using System.CommandLine;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Amqp;
using Amqp.Framing;
using AmqpTest;

const string securedScheme = "AMQPS";
const string unsecuredScheme = "AMQP";
const int securedPort = 5671;
const int unsecuredPort = 5672;

const string defaultQueueName = "queue1";
const string defaultTopicName = "topic1";

var storeNameOption = new Option<StoreName>("--storeName", () => StoreName.My, "The store's name");
var storeLocationOption = new Option<StoreLocation>("--storeLocation", () => StoreLocation.CurrentUser, "The store's location");

var secureOption = new Option<bool>("--secure", () => false, $"Attempt an unsecured (False = {unsecuredScheme}) or a secured (True = {securedScheme}) connection");
var hostOption = new Option<string>("--host", "The broker host name") {IsRequired = true};
var portOption = new Option<int?>("--port", $"The broker host port (if not specified, {unsecuredPort} will be assumed for {unsecuredScheme}, {securedPort} for {securedScheme})");
var userOption = new Option<string?>("--user", "The broker user's name");
var passwordOption = new Option<string?>("--password", "The broker user's password");
var rootCertFileNameOption = new Option<string?>("--rootCertFileName", "The file name of a root certificate to install");
var disableServerCertValidationOption = new Option<bool>("--disableServerCertValidation", () => false, "Disable server certificate validation (True) or not (False)");
var addressOption = new Option<string?>("--address", "The name of the queue/topic (if not specified, 'queue1' will be assumed for a queue, 'topic1' for a topic)");
var addressSchemeOption = new Option<AddressScheme>("--addressType", () => AddressScheme.Queue, "The scheme of the address (queue or topic)");
var verboseOption = new Option<bool>("--verbose", () => false, "Displays verbose output (True) or not (False)");

var durableOption = new Option<bool>("--durable", () => false, "Send a durable (True) or non-durable (False) message");
var messageBodyOption = new Option<string>("--messageBody", () => "Hello World!", "The message's body");
var sendCountOption = new Option<int>("--sendCount", () => 1, "The number of identical messages to send");

var receiveCountOption = new Option<int>("--receiveCount", () => 1, "The number of messages to be received before exiting");
var receiveTimeoutSecondsOption = new Option<int>("--receiveTimeoutSeconds", () => -1, "The number of seconds to wait for a message to be available (-1 = wait forever)");

var listX509StoreCommand = new Command("listX509Store", "List the certififcates in an X509 store")
{
    storeNameOption, storeLocationOption
};

var sendCommand = new Command("send", "AMQP message sender")
{
    secureOption, hostOption, portOption, userOption, passwordOption, rootCertFileNameOption, disableServerCertValidationOption, addressOption, addressSchemeOption, verboseOption,
    durableOption, messageBodyOption, sendCountOption
};

var receiveCommand = new Command("receive", "AMQP message receiver")
{
    secureOption, hostOption, portOption, userOption, passwordOption, rootCertFileNameOption, disableServerCertValidationOption, addressOption, addressSchemeOption, verboseOption,
    receiveCountOption, receiveTimeoutSecondsOption
};

var rootCommand = new RootCommand("AMQP test tool") {listX509StoreCommand, sendCommand, receiveCommand};

listX509StoreCommand.SetHandler((StoreName storeName, StoreLocation storeLocation) =>
{
    using var store = new X509Store(storeName, storeLocation);
    store.Open(OpenFlags.ReadOnly);

    foreach (var cert in store.Certificates.OrderBy(c => c.SubjectName.Name))
    {
        Console.WriteLine($"Subject Name '{cert.SubjectName.Name}', Thumbprint '{cert.Thumbprint}'");
    }
}, storeNameOption, storeLocationOption);

sendCommand.SetHandler(
    (Func<bool, string, int?, string?, string?, string?, bool, string, AddressScheme, bool, bool, string, int, Task>)SendCommandHandler, 
    secureOption, hostOption, portOption, userOption, passwordOption, rootCertFileNameOption, disableServerCertValidationOption, addressOption, addressSchemeOption, verboseOption, durableOption, messageBodyOption, sendCountOption
);

receiveCommand.SetHandler(
    (Func<bool, string, int?, string?, string?, string?, bool, string, AddressScheme, bool, int, int, Task>)ReceiveCommandHandler, 
    secureOption, hostOption, portOption, userOption, passwordOption, rootCertFileNameOption, disableServerCertValidationOption, addressOption, addressSchemeOption, verboseOption, receiveCountOption, receiveTimeoutSecondsOption
);

return rootCommand.Invoke(args);

static async Task SharedCommandHandler(bool secure, string host, int? port, string? user, string? password, string? rootCertFileName, bool disableServerCertValidation, bool verbose, Func<Session, Task> performAction)
{
    Connection? connection = null;
    Session? session = null;

    try
    {
        Connection.DisableServerCertValidation = disableServerCertValidation;
        ProcessRootCertFileName(rootCertFileName, verbose);
        var factory = GetConnectionFactory(verbose);

        var address = new Address(host, port ?? (secure ? securedPort : unsecuredPort), user, password, "/", (secure ? securedScheme : unsecuredScheme).ToLower());
        
        if (verbose)
        {Console.WriteLine($"Connecting to {address.Scheme}://{(user is not null && password is not null ? $"{address.User}:{new string('*', address.Password.Length)}@" : string.Empty)}{address.Host}:{address.Port}{address.Path}");}

        connection = await factory.CreateAsync(address).ConfigureAwait(false);
        session = new Session(connection);
        await performAction(session).ConfigureAwait(false);
    }
    catch (Exception ex) {Console.WriteLine($"Error: {ex.Message}");}
    finally
    {
        if (session is not null) {await session.CloseAsync().ConfigureAwait(false);}
        if (connection is not null) {await connection.CloseAsync().ConfigureAwait(false);}
    }
}

static async Task SendCommandHandler(bool secure, string host, int? port, string? user, string? password, string? rootCertFileName, bool disableServerCertValidation, string? address, AddressScheme scheme, bool verbose, bool durable, string messageBody, int sendCount)
{
    await SharedCommandHandler(secure, host, port, user, password, rootCertFileName, disableServerCertValidation, verbose, async session =>
    {
        SenderLink? sender = null;

        try
        {
            var sendAddress = GetAddress(address, scheme);
            if (verbose) {Console.WriteLine($"Sending to {sendAddress}");}

            sender = new SenderLink(session, $"sender_{Guid.NewGuid()}", sendAddress);
            using var message = new Message(messageBody);
            message.Header = new Header {Durable = durable};

            for (var i = 0; i < sendCount; i++)
            {
                await sender.SendAsync(message).ConfigureAwait(false);
                if (verbose) {Console.WriteLine($"Message {i + 1} sent");}
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

static async Task ReceiveCommandHandler(bool secure, string host, int? port, string? user, string? password, string? rootCertFileName, bool disableServerCertValidation, string? address, AddressScheme scheme, bool verbose, int receiveCount, int receiveTimeoutSeconds)
{
    await SharedCommandHandler(secure, host, port, user, password, rootCertFileName, disableServerCertValidation, verbose, async session =>
    {
        ReceiverLink? receiver = null;

        try
        {
            var receiveAddress = GetAddress(address, scheme);
            if (verbose) {Console.WriteLine($"Receiving from {receiveAddress}");}
            receiver = new ReceiverLink(session, $"receiver_{Guid.NewGuid()}", receiveAddress);
            
            for (var i = 0; i < receiveCount; i++)
            {
                using var message = await receiver.ReceiveAsync(receiveTimeoutSeconds == -1 ? Timeout.InfiniteTimeSpan : TimeSpan.FromSeconds(receiveTimeoutSeconds)).ConfigureAwait(false);
                receiver.Accept(message);
                if (verbose) {Console.WriteLine($"Message {i + 1} received");}
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

static void ProcessRootCertFileName(string? rootCertFileName, bool verbose)
{
    if (File.Exists(rootCertFileName))
    {
        using var store = new X509Store(StoreName.Root, StoreLocation.CurrentUser);
        store.Open(OpenFlags.ReadOnly);
        using var cert = new X509Certificate2(rootCertFileName);

        // ReSharper disable once AccessToDisposedClosure
        if (store.Certificates.All(c => c.Thumbprint != cert.Thumbprint))
        {
            store.Open(OpenFlags.ReadWrite);
            store.Add(cert);

            if (verbose) {Console.WriteLine($"Certificate with thumbprint '{cert.Thumbprint}' added to store name '{StoreName.Root}', store location '{StoreLocation.LocalMachine}'");}
        }
        else if (verbose) {Console.WriteLine($"Certificate with thumbprint '{cert.Thumbprint}' already exists in store name '{StoreName.Root}', store location '{StoreLocation.LocalMachine}'");}

        store.Close();
    }
    else if (rootCertFileName is not null) {Console.WriteLine($"File '{rootCertFileName}' does not exist");}
}

static ConnectionFactory GetConnectionFactory(bool verbose)
{
    var factory = new ConnectionFactory();

    factory.SSL.RemoteCertificateValidationCallback += (_, _, chain, errors) =>
    {
        var valid = true;

        // ReSharper disable once InvertIf
        if (!Connection.DisableServerCertValidation)
        {
            // ReSharper disable once InvertIf
            if (chain is not null)
            {
                if (verbose)
                {
                    foreach (var cert in chain.ChainElements.Select(el => el.Certificate))
                    {Console.WriteLine($"Chain includes cert with subject name '{cert.SubjectName.Name}', issuer '{cert.IssuerName.Name}', and thumbprint '{cert.Thumbprint}'");}
                }

                // ReSharper disable once InvertIf
                if (errors != SslPolicyErrors.None)
                {
                    valid = false;
                    Console.WriteLine($"Errors: {errors}");
                }
            }
        }

        return valid;
    };

    return factory;
}

static string GetAddress(string? address, AddressScheme scheme) => $"{scheme.ToString().ToLower()}://{address ?? (scheme == AddressScheme.Queue ? defaultQueueName : scheme == AddressScheme.Topic ? defaultTopicName : Guid.NewGuid().ToString())}";