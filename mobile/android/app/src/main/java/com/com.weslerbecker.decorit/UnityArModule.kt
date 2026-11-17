package com.weslerbecker.decorit

import android.content.Intent
import android.content.Context
import android.app.Activity
import com.facebook.react.bridge.BaseActivityEventListener
import com.facebook.react.bridge.Promise
import com.facebook.react.bridge.ReactApplicationContext
import com.facebook.react.bridge.ReactContextBaseJavaModule
import com.facebook.react.bridge.ReactMethod
import com.facebook.react.bridge.ReadableMap

class UnityArModule(private val context: ReactApplicationContext) : ReactContextBaseJavaModule(context) {
  private var catalogPromise: Promise? = null
  private val catalogRequest = 41872
  init {
    context.addActivityEventListener(object : BaseActivityEventListener() {
      override fun onActivityResult(activity: Activity, requestCode: Int, resultCode: Int, data: Intent?) {
        if (requestCode != catalogRequest) return
        val pending = catalogPromise ?: return
        catalogPromise = null
        pending.resolve(if (resultCode == Activity.RESULT_OK) data?.getStringExtra("decorit_catalog_result") else null)
      }
    })
  }
  override fun getName() = "UnityArModule"

  @ReactMethod
  fun openCatalogSelection(draftJson: String, promise: Promise) {
    try {
      check(catalogPromise == null) { "O catálogo já está aberto." }
      val activity = context.currentActivity ?: throw IllegalStateException("O app não está em primeiro plano.")
      val intent = UnityArLauncher.catalogSelectionIntent(activity, draftJson)
      catalogPromise = promise
      activity.startActivityForResult(intent, catalogRequest)
    } catch (error: Exception) {
      catalogPromise = null
      promise.reject("UNITY_CATALOG_START_FAILED", error.message, error)
    }
  }

  @ReactMethod
  fun loadCurtainSettings(promise: Promise) {
    try { promise.resolve(ArSettingsStore.load(context)) }
    catch (error: Exception) { promise.reject("AR_SETTINGS_READ_FAILED", error.message, error) }
  }

  @ReactMethod
  fun openCatalogPreview(promise: Promise) {
    try {
      val activity = context.currentActivity ?: throw IllegalStateException("O app não está em primeiro plano.")
      UnityArLauncher.openCatalogPreview(activity)
      promise.resolve(null)
    } catch (error: Exception) {
      promise.reject("UNITY_CATALOG_START_FAILED", error.message, error)
    }
  }

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
        payload.hasKey("quickAR") && payload.getBoolean("quickAR"),
      )
      promise.resolve(null)
    } catch (error: Exception) {
      promise.reject("UNITY_AR_START_FAILED", error.message, error)
    }
  }
}

object UnityArLauncher {
  private fun isDarkTheme(context: Context): Boolean = runCatching {
    android.database.sqlite.SQLiteDatabase.openDatabase(context.getDatabasePath("decorit.db").path, null, android.database.sqlite.SQLiteDatabase.OPEN_READONLY).use { db ->
      db.rawQuery("SELECT value FROM app_meta WHERE key = 'ui_theme'", null).use { cursor ->
        cursor.moveToFirst() && cursor.getString(0) == "dark"
      }
    }
  }.getOrDefault(false)

  fun openCatalogPreview(context: Context) {
    val intent = Intent(context, ImmersiveUnityPlayerActivity::class.java)
      .putExtra("decorit_rn_host", true)
      .putExtra("decorit_catalog_preview", true)
      .putExtra("decorit_dark_theme", isDarkTheme(context))
    context.startActivity(intent)
  }

  fun catalogSelectionIntent(context: Context, draftJson: String): Intent =
    Intent(context, ImmersiveUnityPlayerActivity::class.java)
      .putExtra("decorit_rn_host", true)
      .putExtra("decorit_catalog_selection", true)
      .putExtra("decorit_draft_json", draftJson)
      .putExtra("decorit_dark_theme", isDarkTheme(context))

  fun open(context: Context, clientId: String?, roomId: String?, recordId: String?, draftJson: String?, quickAR: Boolean = false) {
    val intent = Intent(context, ImmersiveUnityPlayerActivity::class.java)
      .putExtra("decorit_rn_host", true)
      .putExtra("decorit_client_id", clientId)
      .putExtra("decorit_room_id", roomId)
      .putExtra("decorit_record_id", recordId)
      .putExtra("decorit_draft_json", draftJson)
      .putExtra("decorit_quick_ar", quickAR)
      .putExtra("decorit_dark_theme", isDarkTheme(context))
    context.startActivity(intent)
  }
}
