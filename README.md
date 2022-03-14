A basic AMQP 1.0 test tool for both sending and receiving secured/unsecured messages. The code is written in .NET 6 (C#) using Visual Studio 2022 and has been tested under both *linux-x64* and *win-x64*.

Docker [image](https://hub.docker.com/repository/docker/danbrill/amqp-test).

# Summary

**Description:**<br>
AMQP test tool<br>

**Usage:**<br>
``amqp-test [command] [options]``<br>

**Options:**<br>
``--version`` Show version information<br>
``-?``, ``-h``, ``--help`` Show help and usage information<br>

**Commands:**<br>
``listX509Store`` List the certififcates in an X509 store<br>
``send`` AMQP message sender<br>
``receive`` AMQP message receiver<br>

## listX509Store<br>

**Description:**<br>
List the certififcates in an X509 store<br>

**Usage:**<br>
``amqp-test listX509Store [options]``<br>

**Options:**<br>
``--storeName`` The store's name [default: ``My``]
* ``AddressBook``
* ``AuthRoot``
* ``CertificateAuthority``
* ``Disallowed``
* ``My``
* ``Root``
* ``TrustedPeople``
* ``TrustedPublisher``

``--storeLocation`` The store's location [default: ``CurrentUser``]
* ``CurrentUser``
* ``LocalMachine``

``-?``, ``-h``, ``--help`` Show help and usage information

**Examples:**<br>
``amqp-test listX509Store --storeName My --storeLocation CurrentUser``

## send

**Description:**<br>
AMQP message sender<br>

**Usage:**<br>
``amqp-test send [options]``<br>

**Options:**<br>
``--secure`` Attempt an unsecured (False/missing = AMQP) or a secured (True = AMQPS) connection [default: ``False``]<br>
``--host <host>`` **(REQUIRED)** The broker host name<br>
``--port <port>`` The broker host port (if not specified, 5672 will be assumed for AMQP, 5671 for AMQPS)<br>
``--user <user>`` The broker user's name<br>
``--password <password>`` The broker user's password<br>
``--rootCertFileName <rootCertFileName>`` The file name of a root certificate to install<br>
``--disableServerCertValidation`` Disable server certificate validation (True) or not (False) [default: ``False``]<br>
``--address <address>`` The name of the queue/topic (if not specified, 'queue1' will be assumed for a queue, 'topic1' for a topic)<br>
``--addressType <Queue|Topic>`` The scheme of the address (queue or topic) [default: ``Queue``]<br>
``--verbose`` Displays verbose output (True) or not (False) [default: ``False``]<br>
``--durable`` Send a durable (True) or non-durable (False) message [default: ``False``]<br>
``--messageBody <messageBody>`` The message's body [default: ``Hello World!``]<br>
``--sendCount <sendCount>`` The number of identical messages to send [default: ``1``]<br>
``-?``, ``-h``, ``--help`` Show help and usage information<br>

**Examples:**<br>
``amqp-test send --host activemq --user admin --password admin``<br>
``amqp-test send --secure --host activemq --user admin --password admin --rootCertFileName ca.crt``<br>

## receive

**Description:**<br>
AMQP message receiver<br>

**Usage:**<br>
``amqp-test receive [options]``<br>

**Options:**<br>
``--secure`` Attempt an unsecured (False/missing = AMQP) or a secured (True = AMQPS) connection [default: ``False``]<br>
``--host <host>`` (REQUIRED) The broker host name<br>
``--port <port>`` The broker host port (if not specified, 5672 will be assumed for AMQP, 5671 for AMQPS)<br>
``--user <user>`` The broker user's name<br>
``--password <password>`` The broker user's password<br>
``--rootCertFileName <rootCertFileName>`` The file name of a root certificate to install<br>
``--disableServerCertValidation`` Disable server certificate validation (True) or not (False) [default: ``False``]<br>
``--address <address>`` The name of the queue/topic (if not specified, 'queue1' will be assumed for a queue, 'topic1' for a topic)<br>
``--addressType <Queue|Topic>`` The scheme of the address (queue or topic) [default: ``Queue``]<br>
``--verbose`` Displays verbose output (True) or not (False) [default: ``False``]<br>
``--receiveCount <receiveCount>`` The number of messages to be received before exiting [default: ``1``]<br>
``--receiveTimeoutSeconds <receiveTimeoutSeconds>`` The number of seconds to wait for a message to be available (-1 = wait forever) [default: ``-1``]<br>
``-?``, ``-h``, ``--help`` Show help and usage information<br>

**Examples:**<br>
``amqp-test receive --host activemq --user admin --password admin``<br>
``amqp-test receive --secure --host activemq --user admin --password admin --rootCertFileName ca.crt``<br>
