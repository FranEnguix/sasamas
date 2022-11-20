using Matrix;
using Matrix.Network.Resolver;
using Matrix.Network;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Matrix.Xmpp;
using Matrix.Extensions.Client.Roster;
using Matrix.Extensions.Client.Presence;

public class SimulationController : MonoBehaviour
{
    private XmppClient x;
    private async void Start() {
        Debug.Log("peticion lanzada?");
        x = new XmppClient {
            Username = "agente1",
            Password = "xmppserver",
            XmppDomain = "localhost",
            HostnameResolver = new StaticNameResolver("127.0.0.1"),
            CertificateValidator = new AlwaysAcceptCertificateValidator(),
            Tls = false
        };

        await x.ConnectAsync();

        // request roster (contact list)
        var roster = await x.RequestRosterAsync();
        Debug.Log(roster);

        // send own presence to the server
        await x.SendPresenceAsync(Show.Chat, "free for Chat");

        Debug.Log("Conseguidooooo XMOOOOPP");
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            Application.Quit();
    }
}
