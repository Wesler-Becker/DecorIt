package com.weslerbecker.decorit

import android.content.Intent
import android.content.Context
import com.facebook.react.bridge.Promise
import com.facebook.react.bridge.ReactApplicationContext
import com.facebook.react.bridge.ReactContextBaseJavaModule
import com.facebook.react.bridge.ReactMethod
import com.facebook.react.bridge.ReadableMap

class UnityArModule(private val context: ReactApplicationContext) : ReactContextBaseJavaModule(context) {
  override fun getName() = "UnityArModule"

  @ReactMethod
  fun openAR(payload: ReadableMap, promise: Promise) {
    try {
      val activity = context.currentActivity ?: throw IllegalStateException("O app não está em primeiro plano.")
      UnityArLauncher.open(
        activity,
        payload.getString("clientId"),
        payload.getString("roomId"),
        payload.getString("recordId"),
        payload.getString("draftJson"),
      )
      promise.resolve(null)
    } catch (error: Exception) {
      promise.reject("UNITY_AR_START_FAILED", error.message, error)
    }
  }
}

object UnityArLauncher {
  fun open(context: Context, clientId: String?, roomId: String?, recordId: String?, draftJson: String?) {
    val intent = Intent(context, ImmersiveUnityPlayerActivity::class.java)
      .putExtra("decorit_rn_host", true)
      .putExtra("decorit_client_id", clientId)
      .putExtra("decorit_room_id", roomId)
      .putExtra("decorit_record_id", recordId)
      .putExtra("decorit_draft_json", draftJson)
    context.startActivity(intent)
  }
}
