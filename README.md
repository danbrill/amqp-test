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
``docker run --rm danbrill/amqp-test:1.0.4 listX509Store --storeName My --storeLocation CurrentUser``

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
``--disableServerCertValidation`` Disable server certificate validition or not [default: ``False``]<br>
``--queueName <queueName>`` The name of the queue/topic [default: ``queue1``]<br>
``--verbose`` Displays verbose output or not [default: ``False``]<br>
``--senderName <senderName>`` The sender's name [default: ``sender1``]<br>
``--messageBody <messageBody>`` The message's body [default: ``Hello World!``]<br>
``--sendCount <sendCount>`` The number of identical messages to send [default: ``1``]<br>
``-?``, ``-h``, ``--help`` Show help and usage information<br>

**Examples:**<br>
``docker run --rm danbrill/amqp-test:1.0.4 send --host activemq --user admin --password admin``<br>
``docker run --mount "type=bind,source=$pwd,target=/tmp/ssl/certs" --rm danbrill/amqp-test:1.0.4 send --secure --host activemq --user admin --password admin --rootCertFileName /tmp/ssl/certs/ca.crt``<br>

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
``--disableServerCertValidation`` Disable server certificate validition or not [default: ``False``]<br>
``--queueName <queueName>`` The name of the queue/topic [default: ``queue1``]<br>
``--verbose`` Displays verbose output or not [default: ``False``]<br>
``--receiverName <receiverName>`` The receiver's name [default: ``receiver1``]<br>
``--receiveCount <receiveCount>`` The number of messages to be received before exiting [default: ``1``]<br>
``--receiveTimeoutSeconds <receiveTimeoutSeconds>`` The number of seconds to wait for a message to be available (-1 = wait forever) [default: ``-1``]<br>
``-?``, ``-h``, ``--help`` Show help and usage information<br>

**Examples:**<br>
``docker run --rm danbrill/amqp-test:1.0.4 receive --host activemq --user admin --password admin``<br>
``docker run --mount "type=bind,source=$pwd,target=/tmp/ssl/certs" --rm danbrill/amqp-test:1.0.4 receive --secure --host activemq --user admin --password admin --rootCertFileName /tmp/ssl/certs/ca.crt``<br>
