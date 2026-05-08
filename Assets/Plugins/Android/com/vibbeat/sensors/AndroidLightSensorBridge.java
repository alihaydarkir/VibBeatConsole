package com.vibbeat.sensors;

import android.content.Context;
import android.hardware.Sensor;
import android.hardware.SensorEvent;
import android.hardware.SensorEventListener;
import android.hardware.SensorManager;
import com.unity3d.player.UnityPlayer;

/**
 * VibBeat Android Işık Sensörü Köprüsü
 *
 * Senaryo:
 *   - Sol el sensörü kapatır → min lux (kalibrasyon 0 noktası)
 *   - El çekilir, sensör ortam ışığına açılır → max lux (kalibrasyon 1 noktası)
 *
 * Raw lux değeri Unity'ye iletilir.
 * Normalizasyon ve kalibrasyon: CalibrationManager.cs tarafında yapılır.
 */
public class AndroidLightSensorBridge implements SensorEventListener
{
    // ─────────────────────────────────────────
    // SABITLER
    // ─────────────────────────────────────────
    private static final String UNITY_OBJECT        = "SensorController";
    private static final String UNITY_METHOD_LUX    = "OnLuxValueChanged";
    private static final String UNITY_METHOD_STATUS = "OnSensorStatusChanged";

    // Exponential Moving Average katsayısı.
    // 0.25f ≈ 4 örnek gecikmeli yumuşatma — ani el hareketlerini filtreler
    private static final float SMOOTHING_ALPHA = 0.25f;

    // ─────────────────────────────────────────
    // SINGLETON — Thread-safe double-checked locking
    // ─────────────────────────────────────────
    private static volatile AndroidLightSensorBridge instance = null;

    public static AndroidLightSensorBridge getInstance()
    {
        if (instance == null)
        {
            synchronized (AndroidLightSensorBridge.class)
            {
                if (instance == null)
                {
                    Context ctx = UnityPlayer.currentActivity.getApplicationContext();
                    instance = new AndroidLightSensorBridge(ctx);
                }
            }
        }
        return instance;
    }

    // ─────────────────────────────────────────
    // ALANLAR
    // ─────────────────────────────────────────
    private final SensorManager sensorManager;
    private final Sensor        lightSensor;

    private float   rawLux       = 0f;
    private float   smoothedLux  = 0f;
    private boolean isListening  = false;
    private boolean sensorExists = false;

    // ─────────────────────────────────────────
    // KURUCU
    // ─────────────────────────────────────────
    private AndroidLightSensorBridge(Context context)
    {
        sensorManager = (SensorManager) context.getSystemService(Context.SENSOR_SERVICE);
        lightSensor   = sensorManager.getDefaultSensor(Sensor.TYPE_LIGHT);
        sensorExists  = (lightSensor != null);

        if (!sensorExists)
            sendStatus("ERROR:NO_LIGHT_SENSOR");
        else
            sendStatus("READY");
    }

    // ─────────────────────────────────────────
    // DINLEME KONTROLU
    // ─────────────────────────────────────────

    /**
     * SENSOR_DELAY_GAME ≈ 20ms — ses gecikmesini minimize eder.
     * SENSOR_DELAY_UI  ≈ 60ms — müzik uygulaması için çok yavaş olurdu.
     */
    public void startListening()
    {
        if (isListening || !sensorExists) return;

        boolean ok = sensorManager.registerListener(
            this, lightSensor, SensorManager.SENSOR_DELAY_GAME
        );

        isListening = ok;
        sendStatus(ok ? "LISTENING" : "ERROR:REGISTER_FAILED");
    }

    public void stopListening()
    {
        if (!isListening) return;
        sensorManager.unregisterListener(this);
        isListening = false;
        sendStatus("STOPPED");
    }

    // ─────────────────────────────────────────
    // SENSOR CALLBACK
    // ─────────────────────────────────────────
    @Override
    public void onSensorChanged(SensorEvent event)
    {
        if (event.sensor.getType() != Sensor.TYPE_LIGHT) return;

        rawLux = event.values[0];

        // EMA filtresi — ani sıçramaları yumuşatır
        smoothedLux = SMOOTHING_ALPHA * rawLux + (1f - SMOOTHING_ALPHA) * smoothedLux;

        // Locale.US → ondalık ayracı her zaman nokta
        UnityPlayer.UnitySendMessage(
            UNITY_OBJECT,
            UNITY_METHOD_LUX,
            String.format(java.util.Locale.US, "%.2f", smoothedLux)
        );
    }

    @Override
    public void onAccuracyChanged(Sensor sensor, int accuracy)
    {
        sendStatus("ACCURACY:" + accuracy);
    }

    // ─────────────────────────────────────────
    // YARDIMCI
    // ─────────────────────────────────────────
    private void sendStatus(String status)
    {
        UnityPlayer.UnitySendMessage(UNITY_OBJECT, UNITY_METHOD_STATUS, status);
    }

    public float getRawLux()      { return rawLux; }
    public float getSmoothedLux() { return smoothedLux; }
    public boolean isListening()  { return isListening; }
    public boolean hasSensor()    { return sensorExists; }
}
