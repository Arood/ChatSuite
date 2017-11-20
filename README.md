Chat Suite is a collection of small utilities to make your server chat better in Rust. It requires that your server is running Oxide. See the automatically generated configuration file for examples of each feature.

Features:

1. Welcome message
2. Join/Disconnect messages
3. Airdrop and Patrol Helicopter notifications
4. `/players` command
5. `/say` command for permitted users
6. Advertisements (automatic messages every X minutes)
7. Custom commands

**Welcome message**

A customizable welcome message is shown to the player each time they connect to the server. You can also preview the message after changing it with the `/welcome` command.

**Join/Disconnect**

Broadcasts a message each time a player joins or disconnects from the server. The messages can be customized in the language files.

**Airdrop and Patrol Helicopter notifications**

Each time an airdrop or patrol helicopter is spawned, a message will be broadcasted. You can disable each of these individually.

Note: By default the Airdrop notification will show coordinates where the drop will be landing. You can remove this by changing the message in the language files.

**/players command**

Typing `/players` will show all players that are currently online.

**/say command for permitted users**

The `/say` command will let you communicate with your players anonymously as ADMIN (the name is customizable) in the chat. To use the command, you will need the `chatsuite.adminsay` permission in Oxide.

**Advertisements (automatic messages every X minutes)**

Shows a message every X minute from a list of messages in your configuration. You can also set the interval between each message.

**Custom commands**

Lets you register custom chat commands users can use to display information in the chat. There are two types of commands, if the command starts with `/`, the command won't be shown in the chat, but if it starts with `!` it will (so other players will know how to trigger the command themselves).

Each command has two properties that needs to be set.

1. `broadcast`: If `true`, the message will be shown to all players. If `false`, the message will only be shown to the player sending the command.
2. `text`: A list of texts that will be sent as the message. Each item in the list equals to one message (with profile icon).
