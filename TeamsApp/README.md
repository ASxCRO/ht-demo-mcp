# HT Voice Support for Microsoft Teams

This is a configurable Teams channel/group chat tab, not a calling bot or meeting participant. The existing public ElevenLabs agents are embedded using the widget; no API key is sent to the browser. Use synthetic data only. Each tab starts a separate voice session; there is no automatic agent-to-agent audio bridge.

## Deploy
The existing ASP.NET Core host serves wwwroot/teams under https://ht-demo-mcp.onrender.com/teams/. Verify /teams/index.html?agent=sara and ?agent=customer in a browser and check microphone. The /mcp Bearer authentication remains unchanged.

## Install
1. Run python3 TeamsApp/build_package.py.
2. Teams > Apps > Manage your apps > Upload an app > Upload a custom app. Select ht-voice-teams.zip. Your tenant must allow custom app uploads.
3. In a standard team channel, click + (Add a tab), select HT Voice Support, choose Sara or Ana Anić – korisnica, and Save.
4. Repeat for the second agent. Grant microphone access from the app permissions or prompt. If desktop mic fails, try Teams web or Open in browser.
5. If custom upload is unavailable, ask the tenant admin to approve/publish the package via Teams admin center, or try the Website tab with the two direct HTTPS URLs (embedding/microphone may be limited).

For real customer data, replace public widget access with tenant authentication and server-side scoped conversation credentials; don't use this public widget setup.

Docs:
https://elevenlabs.io/docs/eleven-agents/phone-numbers/microsoft-teams/widget-tab
https://learn.microsoft.com/en-us/microsoftteams/platform/tabs/how-to/create-tab-pages/configuration-page
