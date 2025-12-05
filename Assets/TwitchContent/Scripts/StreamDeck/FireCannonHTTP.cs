using System.Net;

public class FireCannonHTTP : HTTPListener
{
    protected override void OnRequestReceived(HttpListenerContext context)
    {
        FireballCannon fireballCannon = FindObjectOfType<FireballCannon>();
        fireballCannon.ImmediateFireCannon();
    }
}
