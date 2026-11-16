# Game Backend: Photon Server, MySQL and PlayFab

Two game-backend projects from my Server Development module at Nanyang Polytechnic (2021–22). Together they cover a **self-hosted** backend (Photon Server plugin with MySQL) and a **cloud** backend (Microsoft PlayFab), each with a Unity client.

| Folder | What it is |
|---|---|
| [`photon-plugin/`](photon-plugin) | C# **Photon Server plugin** that handles game events on the server and stores player data in **MySQL** |
| [`photon-client/`](photon-client) | Unity client for the plugin: account login, **Facebook device login**, inventory, in-game mail and password reset by email |
| [`database/`](database) | MySQL schema (table structure only, no data) |
| [`playfab-client/`](playfab-client) | Unity client using **PlayFab**: register and login, friends list, leaderboards, player profiles and a lobby |
| [`mysql-console/`](mysql-console) | Small C# console app used to practise MySQL queries from .NET |

## Part 1: Photon Server plugin + MySQL

The plugin (`MyFirstPlugin.cs`) extends `PluginBase` and handles custom `RaiseEvent` codes from the client:

- **Account flow:** sign up, log in, disconnect and password reset, plus login with a Facebook account
- **Persistence:** reads and writes player data and inventory items in MySQL through a small `Database` helper (MySql.Data)
- **Discord webhook:** posts a welcome message to a Discord channel when a new player registers
- **Server responses:** sends results back to the calling client with `BroadcastEvent`

The Unity client (`photon-client/Assets/Scipts`) connects with Photon PUN 2 and includes:
`Launcher` (connect and login UI), `FacebookHandler` (Facebook device-code login through the Graph API), `Inventory`/`Item`, `SendMail` (password reset code sent by email over SMTP) and `RaiseEventListener`.

### Setup

1. Download the **Photon Server Plugin SDK v5** (not included in this repo). Copy the `photon-plugin` folder to `<SDK>/src-server/Plugins/MyFirstPlugin`, because the project references the SDK's `../lib/net461/PhotonHivePlugin.dll`. Then build `MyFirstPlugin.sln` (.NET Framework 4.7.2). NuGet restores the other packages automatically.
2. Copy `MyFirstPlugin.dll` and `plugin.config` into the SDK's `deploy/Plugins` folder.
3. Create the database: `mysql -u root -p < database/schema.sql`
4. **Set your MySQL password.** The real password has been removed from this repo. Replace the `"YOUR_DB_PASSWORD"` placeholder with your own MySQL root password in:
   - `photon-plugin/MyFirstPlugin.cs` (the `db.Connect(...)` call in the constructor)
   - `mysql-console/Program.cs` (the `db.Connect(...)` call in `Main`)

   Don't commit your real password back to the repo.
   - Optional: set the `DISCORD_WEBHOOK_URL` environment variable to a Discord webhook URL for the welcome message.
5. In `photon-client`, fill in your own values:
   - `Assets/Scipts/FacebookHandler.cs`: `YOUR_FB_APP_ID|YOUR_FB_CLIENT_TOKEN`
   - `Assets/Scipts/SendMail.cs`: sender Gmail address and an App Password
6. Open `photon-client` in Unity 2020.3 and run the `Launcher` scene.

## Part 2: PlayFab

`playfab-client/Assets/Scripts` contains:

- `PlayfabRegister` / `PlayfabLogin`: account creation and login
- `PlayfabFriends`: add and list friends
- `PlayFabLeaderboard`: submit scores and show the leaderboard
- `LobbyManager`, plus profile, friend and leaderboard UI prefabs

### Setup

1. Create a title in [PlayFab Game Manager](https://developer.playfab.com/).
2. In Unity, open **Window → PlayFab → Editor Extensions** and log in. Alternatively, set your **Title ID** in `Assets/PlayFabSDK/Shared/Public/Resources/PlayFabSharedSettings.asset`.
3. Open `Assets/Scenes/LoginScene.unity` and press Play.

> Never commit your PlayFab *Developer Secret Key*. It is only needed for server or admin API calls.

## Notes and limitations

This was coursework focused on learning the backend services, so a few things would change in production:

- Passwords are stored in plain text, and SQL queries are built with string interpolation. A real version would hash passwords (for example with bcrypt) and use parameterised queries.
- All credentials (database, Discord, Facebook, SMTP, PlayFab, Photon) have been replaced with placeholders or environment variables. Use your own.
- The original `GameScene` UI used some third-party game sprites. For copyright reasons they have been replaced with simple original placeholder icons (`coin`, `coin_bag`, `mushroom`).

## Tech

C#, .NET Framework 4.7.2, Photon Server SDK v5, Photon PUN 2, MySQL 8 (MySql.Data), Unity 2020.3.23f1, Facebook Graph API, PlayFab SDK.
