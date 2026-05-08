package com.vibbeat.sensors;

import android.content.Context;
import android.hardware.Sensor;
import android.hardware.SensorEvent;
import android.hardware.SensorEventListener;
import android.hardware.SensorManager;
import com.unity3d.player.UnityPlayer;

public class AndroidLightSensorBridge implements SensorEventListener
{
    // --- Singleton ---
    private static AndroidLightSensorBridge instance = null;

    // --- Android Sensor ---
    private SensorManager sensorManager;
    private Sensor        lightSensor;
    private float         currentLuxValue = 0f;
    private boolean       isListening     = false;

    // --- Unity Callback ---
    private static final String UNITY_OBJECT   = "SensorController";
    private static final String UNITY_METHOD   = "OnLuxValueChanged";

    // ─────────────────────────────────────────
    // SINGLETON
    // ─────────────────────────────────────────
    private AndroidLightSensorBridge(Context context)
    {
        sensorManager = (SensorManager) context.getSystemService(
            Context.SENSOR_SERVICE
        );
        lightSensor = sensorManager.getDefaultSensor(Sensor.TYPE_LIGHT);

        if (lightSensor == null)
        {
            UnityPlayer.UnitySendMessage(
                UNITY_OBJECT, 
                "OnLuxValueChanged", 
                "ERROR:NO_SENSOR"
            );
        }
    }

    public static AndroidLightSensorBridge getInstance()
    {
        if (instance == null)
        {
            Context context = UnityPlayer.currentActivity
                                         .getApplicationContext();
            instance = new AndroidLightSensorBridge(context);
        }
        return instance;
    }

    // ─────────────────────────────────────────
    // SENSÖR KONTROLÜ
    // ─────────────────────────────────────────
    public void startListening()
    {
        if (isListening || lightSensor == null) return;

        sensorManager.registerListener(
            this,
            lightSensor,
            SensorManager.SENSOR_DELAY_UI   // ~60ms güncelleme
        );
        isListening = true;
    }

    public void stopListening()
    {
        if (!isListening) return;

        sensorManager.unregisterListener(this);
        isListening = false;
    }

    // ─────────────────────────────────────────
    // SENSOR EVENT LISTENER
    // ─────────────────────────────────────────
    @Override
    public void onSensorChanged(SensorEvent event)
    {
        if (event.sensor.getType() != Sensor.TYPE_LIGHT) return;

        currentLuxValue = event.values[0];

        // Unity'ye gönder
        UnityPlayer.UnitySendMessage(
            UNITY_OBJECT,
            UNITY_METHOD,
            String.valueOf(currentLuxValue)
        );
    }

    @Override
    public void onAccuracyChanged(Sensor sensor, int accuracy)
    {
        // Kullanılmıyor
    }

    // ─────────────────────────────────────────
    // GETTER
    // ─────────────────────────────────────────
    public float getCurrentLux()
    {
        return currentLuxValue;
    }

    public boolean isListening()
    {
        return isListening;
    }
}