# MafiaTown

In MafiaTown we are creating a text-based mafia.

## October 13
- Worked out state diagrams for the client and the server
- Created JSON objects for all messages between client and server
- Discussed data structure for Player object:
```c#
struct Player 
{
    string username;
    string role;
    bool alive;
    int votesAgainst;
    int TOD;
    bool voted;
}
```
- Talked about global variables
```c#
Player[] players;
int current Round;
```

Look at `messages.json` for the types of messages
