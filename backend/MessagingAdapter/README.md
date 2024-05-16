# DIAM Notification Adapter

## Process incoming AWS messages and adapt and produce to internal message bus

This application will receive messages from AWS SQS (and push test messages to SNS).
When a message is received the service will place a new kafka message onto a topic that can then be consumed by other services.

This separates out the AWS messaging from the DIAM messaging and allows other Justice apps to process those messages from Kafka without having to consume from AWS.
