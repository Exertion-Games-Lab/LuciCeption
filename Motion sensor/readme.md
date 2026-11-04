The motion sensor would send a motion detection command to the server, and the server would send the signal to the Unity exe.
















### Create IFTTT Applet

To do this, create IFTTT applet like this:
If: Motion detected or no motion detected: [your device] Motion Detected


Then: make a webrequest: http://[your server]:8080/set_motion_detected?key=xglluciception&participant=p1&value=true

### Deploy the Server

The server code is in `app.py`. The following instructions use Windows PowerShell.

1. Install Python 3 on the computer that will host the server.
2. Open PowerShell in the `Motion sensor` folder and run:

   ```powershell
   py -m venv .venv
   .\.venv\Scripts\Activate.ps1
   python -m pip install Flask waitress
   waitress-serve --listen=0.0.0.0:8080 app:app
   ```

   `app:app` loads the Flask application named `app` from `app.py`. Keep the process running; closing the terminal will stop the server. For production, use Waitress as above rather than running `python app.py`, which starts Flask's development server.

3. Check that the server is running by opening `http://127.0.0.1:8080/` on the host computer. It should display `Hello World!`.
4. To receive requests from IFTTT over the internet, make the host reachable from the internet and allow/forward inbound TCP traffic on port `8080` through the firewall and router. Replace `[your server]` in the IFTTT URL with the host's public IP address or domain name.

The server supports participants `p1` and `p2`. To set a participant's motion state, use `value=true` or `value=false` in the `/set_motion_detected` URL. Unity can read the state from:

```text
http://[your server]:8080/get_motion_detected?key=xglluciception&participant=p1
```

The response contains the state as JSON, for example `{"value":true}`. The current key is hard-coded in `app.py`; change it before deployment and use the same key in IFTTT and Unity. Because the key is sent in the URL, use HTTPS for public deployments. Motion states are kept in memory and reset when the server restarts, so run a single server instance.
