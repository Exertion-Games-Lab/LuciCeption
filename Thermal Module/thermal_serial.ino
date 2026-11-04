#define PWMControl 1
#define IN1 2
#define IN2 3

// #include <WiFi.h>
// #include <WiFi.h>
// #include <WebServer.h>
// #include <Ticker.h>

// ---------- Wi-Fi ----------
// const char* ssid     = "LuciCeptionNetwork";
// const char* ssid     = "7wifi";
// const char* password = "40666888";
// const char* ssid     = "ORBI80";
// // const char* ssid     = "iPhone 16";
// const char* password = "classychair864";
// const char* ssid     = "NOKIA-5942";
// const char* password = "37a472adae";

// IPAddress local_IP(192, 168, 1, 183);     // Set your static IP
// IPAddress gateway(192, 168, 1, 1);        // Set your gateway
// IPAddress subnet(255, 255, 255, 0);       // Set your subnet mask
// IPAddress dns(8, 8, 8, 8);                // (Optional) Set your DNS
// // ---------- Web ----------
// WebServer server(80);



enum Mode {
  OFF,
  WARM,
  COOL
};

// void changePeltierDirection(int mode) {
//   switch (mode) {
//     case WARM:
//       digitalWrite(IN1, HIGH);
//       digitalWrite(IN2, LOW);
//       Serial.println("Mode: WARM | IN1=HIGH, IN2=LOW");
//       break;
//     case COOL:
//       digitalWrite(IN1, LOW);
//       digitalWrite(IN2, HIGH);
//       Serial.println("Mode: COOL | IN1=LOW, IN2=HIGH");
//       break;
//     default:
//       digitalWrite(IN1, LOW);
//       digitalWrite(IN2, LOW);
//       Serial.println("Mode: OFF | IN1=LOW, IN2=LOW");
//       break;
//   }
// }



void setThermalMode(String mode) {
  if (mode=="hot") 
  {
    digitalWrite(IN1, LOW);
    digitalWrite(IN2, HIGH);
    Serial.println("Mode: WARM | IN1=HIGH, IN2=LOW");
  }
  else if (mode=="cool")
  {
    digitalWrite(IN1, HIGH);
    digitalWrite(IN2, LOW);
    Serial.println("Mode: COOL | IN1=LOW, IN2=HIGH");
  }
  else
  {
    digitalWrite(IN1, LOW);
    digitalWrite(IN2, LOW);
    Serial.println("Mode: OFF | IN1=LOW, IN2=LOW");
  }
}


void setThermalIntenisty (int intensity)
{
  analogWrite(PWMControl, intensity);
}

int requestedMode = -1;



// Alternates Peltier ON for 10s and OFF for 10s, increasing PWM by 5% each ON cycle using activatePeltier
// void activatePeltierRepeatedly(int mode) {
//   static unsigned long lastChange = 0;
//   static bool isOn = false;
//   static int pwmValue = 0;
//   static int cycle = 0;
//   unsigned long currentMillis = millis();


//   if (mode == -1)
//   {
//     //do nothing, skip
//     return;
//   }
//   else if (mode == OFF)
//   {
//     isOn = false;
//     pwmValue = 0;
//     //turn it off
//     changePeltierDirection(OFF);
//     requestedMode = -1;

//     return;
//   }

//   int pwmStep = (int)(255 * 0.05); // 5% of 255 ≈ 12
//   if (isOn) {
//     if (currentMillis - lastChange >= 10000) {
//       activatePeltier(OFF, 0); // Turn OFF immediately
//       isOn = false;
//       lastChange = currentMillis;
//       Serial.print("Cycle ");
//       Serial.print(cycle);
//       Serial.println(": OFF for 10s");
//     }
//   } else {
//     if (currentMillis - lastChange >= 10000) {
//       pwmValue = min(255, pwmValue + pwmStep);
//       analogWrite(PWMControl, pwmValue);
//       activatePeltier(mode, 10000); // Turn ON for 10s
//       isOn = true;
//       lastChange = currentMillis;
//       cycle++;
//       Serial.print("Cycle ");
//       Serial.print(cycle);
//       Serial.print(": ON for 10s, PWM = ");
//       Serial.println(pwmValue);
//     }
//   }
// }


// Reads serial input (1,2,3) and activates Peltier for 10 seconds
void handleSerialPeltierControl() {

  if (Serial.available() > 0) {
    char input = Serial.read();
    switch (input) {
      case '1':
        requestedMode = OFF;
        
        return;
      case '2':
        requestedMode = WARM;
        break;
      case '3':
        requestedMode = COOL;
        break;
      default:
        // Ignore other input
        break;
    }
      // activatePeltierRepeatedly(requestedMode);
  }
  // activatePeltier(requestedMode, 10000); // 10 seconds
  
}

// // Gradually increases PWM from 0 to 255
// void gradualPWMIncrease(int stepDelay) {
//   static int pwmValue = 0;
//   static unsigned long lastUpdate = 0;
//   unsigned long currentMillis = millis();
//   if (pwmValue < 255 && currentMillis - lastUpdate >= stepDelay) {
//     pwmValue++;
//     analogWrite(PWMControl, pwmValue);
//     lastUpdate = currentMillis;
//   }

// }

// // Activates Peltier direction for a given duration (ms) based on mode (OFF, WARM, COOL). If mode is OFF, turns off immediately.
// void activatePeltier(int mode, unsigned long durationMs) {
//   static int lastMode = OFF;
//   static bool active = false;
//   static unsigned long startTime = 0;
//   if (mode == OFF) {
//     changePeltierDirection(OFF);
//     active = false;
//     lastMode = OFF;
//     return;
//   }
//   if (!active || lastMode != mode) {
//     changePeltierDirection(mode);
//     startTime = millis();
//     active = true;
//     lastMode = mode;
//   } else {
//     if (millis() - startTime >= durationMs) {
//       changePeltierDirection(OFF);
//       active = false;
//       lastMode = OFF;
//     }
//   }
// }




//http handling

// void handleConfigure() {
//   if (server.hasArg("mode")) {
//     String mode = server.arg("mode");
//     if (mode == "warm") {
//       changePeltierDirection(WARM);
//     } else if (mode == "cool") {
//       changePeltierDirection(COOL);
//     } else { // off
//       changePeltierDirection(OFF);
//     }
//     Serial.print("mode: ");
//     Serial.println(mode);
//   }
//   if (server.hasArg("intensity")) {
//     int intensity = server.arg("intensity").toInt(); // expects 0-255
//     analogWrite(PWMControl, intensity);
//     Serial.print("intensity: ");
//     Serial.println(intensity);
//   }
//   server.send(200, "text/plain", "OK");
// }

void ParseCommand(String command)
{
   // Split into mode and intensity
    int commaIndex = command.indexOf(',');
    if (commaIndex > 0) {
      String mode = command.substring(0, commaIndex);
      int intensity = command.substring(commaIndex + 1).toInt();

      Serial.print("Mode: ");
      Serial.print(mode);
      Serial.print(" | Intensity: ");
      Serial.println(intensity);
      setThermalIntenisty(intensity);
      setThermalMode(mode);
    }
    else
    {
      Serial.println("command error");
      setThermalMode("off");
      setThermalIntenisty(0);
    }
}


void setup() {
  pinMode(PWMControl, OUTPUT);
  pinMode(IN1, OUTPUT);
  pinMode(IN2, OUTPUT);
  //delay(900000);
  //analogWrite(PWMControl, 255);
  analogWrite(PWMControl, 0);
  // digitalWrite(IN1, HIGH);
  // digitalWrite(IN2, LOW);
  Serial.begin(9600);
  delay(100);

  // Wi-Fi connect
  // WiFi.mode(WIFI_STA);
  // WiFi.config(local_IP, gateway, subnet, dns);
  // WiFi.begin(ssid, password);
  // Serial.print("Connecting to WiFi");
  // while (WiFi.status() != WL_CONNECTED) {
  //   Serial.print('.');
  //   delay(500);
  // }
  // Serial.println();

  // HTTP routes
  // server.on("/", HTTP_GET, handleRoot);
  // server.on("/configure", HTTP_GET, handleConfigure);
  // server.begin();
  // Serial.println();
  // Serial.print("IP: "); Serial.println(WiFi.localIP());
}



void loop() {
  if (Serial.available() > 0) {
    
    String command = Serial.readStringUntil('\n'); // read until newline
    ParseCommand(command);
  }
  // server.handleClient();
  // Serial.print("IP: "); Serial.println(WiFi.localIP());
  // Serial.println("loop");
  // handleSerialPeltierControl();
  // activatePeltierRepeatedly(requestedMode);
  // analogWrite(PWMControl, 255);
}

