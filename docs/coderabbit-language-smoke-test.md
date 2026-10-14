# Friend invite review language smoke test

This document describes a small Steam friend invitation flow for a review-language check.

1. Create or join a Steam lobby before calling `SteamLobby.InviteFriends()`. When Steam's overlay is enabled, this opens Steam's overlay invite dialog; otherwise, it opens the in-game friend invitation popup.
2. Bind each friend slot to a Steam friend ID and an invitation callback.
3. When the invitation button is clicked, use the active lobby ID to invite that friend.

This document is temporary and does not change the Unity game's behavior.

