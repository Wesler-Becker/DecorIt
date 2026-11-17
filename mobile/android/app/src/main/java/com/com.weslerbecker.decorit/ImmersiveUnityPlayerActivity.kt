package com.weslerbecker.decorit

import android.os.Bundle
import android.graphics.Color
import android.database.sqlite.SQLiteDatabase
import java.util.UUID
import androidx.core.view.WindowCompat
import androidx.core.view.WindowInsetsCompat
import androidx.core.view.WindowInsetsControllerCompat
import com.unity3d.player.UnityPlayerGameActivity

class ImmersiveUnityPlayerActivity : UnityPlayerGameActivity() {
  fun saveCurtainSettings(value: String) { ArSettingsStore.save(this, value) }
  fun loadCurtainSettings(): String = ArSettingsStore.load(this)
  fun saveCapturedPhoto(clientId: String, roomId: String, recordId: String, path: String, roomName: String, capturedAt: String): Boolean {
    val file = getDatabasePath("decorit.db")
    if (!file.exists() || clientId.isBlank() || path.isBlank()) return false
    return try {
      SQLiteDatabase.openDatabase(file.path, null, SQLiteDatabase.OPEN_READWRITE).use { db ->
        db.setForeignKeyConstraintsEnabled(true)
        db.beginTransaction()
        try {
          if (roomId.isNotBlank()) {
            db.rawQuery("SELECT name FROM rooms WHERE id = ? AND client_id = ?", arrayOf(roomId, clientId)).use { cursor ->
              check(cursor.moveToFirst()) { "Cômodo não encontrado" }
            }
          }
          if (recordId.isNotBlank()) {
            db.rawQuery("SELECT id FROM visit_records WHERE id = ? AND room_id = ?", arrayOf(recordId, roomId)).use { cursor ->
              check(cursor.moveToFirst()) { "Registro não encontrado" }
            }
          }
          val effectiveName = if (roomId.isNotBlank()) {
            db.rawQuery("SELECT name FROM rooms WHERE id = ?", arrayOf(roomId)).use { cursor -> cursor.moveToFirst(); cursor.getString(0) }
          } else roomName.ifBlank { "Não informado" }
          db.execSQL("INSERT OR IGNORE INTO photos (id, client_id, room_id, record_id, path, room_name, captured_at, sort_order) VALUES (?, ?, ?, ?, ?, ?, ?, ?)",
            arrayOf<Any?>(UUID.randomUUID().toString(), clientId, roomId.takeIf { it.isNotBlank() }, recordId.takeIf { it.isNotBlank() }, path, effectiveName, capturedAt.takeIf { it.isNotBlank() }, System.currentTimeMillis()))
          if (roomId.isNotBlank()) {
            db.rawQuery("SELECT COUNT(*) FROM photos WHERE room_id = ?", arrayOf(roomId)).use { cursor ->
              cursor.moveToFirst()
              val count = cursor.getInt(0)
              db.execSQL("UPDATE rooms SET image_label = ? WHERE id = ?", arrayOf(if (count == 1) "1 foto" else "$count fotos", roomId))
            }
          }
          db.setTransactionSuccessful()
        } finally { db.endTransaction() }
      }
      true
    } catch (error: Exception) {
      android.util.Log.e("DecorIt", "Falha ao registrar foto no SQLite", error)
      false
    }
  }
  override fun onCreate(savedInstanceState: Bundle?) {
    super.onCreate(savedInstanceState)
    WindowCompat.setDecorFitsSystemWindows(window, false)
    window.statusBarColor = Color.TRANSPARENT
    window.navigationBarColor = Color.TRANSPARENT
    hideSystemBars()
  }

  override fun onResume() {
    super.onResume()
    hideSystemBars()
  }

  override fun onWindowFocusChanged(hasFocus: Boolean) {
    super.onWindowFocusChanged(hasFocus)
    if (hasFocus) hideSystemBars()
  }

  private fun hideSystemBars() {
    WindowInsetsControllerCompat(window, window.decorView).apply {
      systemBarsBehavior = WindowInsetsControllerCompat.BEHAVIOR_SHOW_TRANSIENT_BARS_BY_SWIPE
      hide(WindowInsetsCompat.Type.systemBars())
    }
  }
}
