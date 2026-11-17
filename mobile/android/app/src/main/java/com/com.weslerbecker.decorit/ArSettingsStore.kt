package com.weslerbecker.decorit

import android.content.Context
import android.database.sqlite.SQLiteDatabase
import org.json.JSONArray
import org.json.JSONObject

object ArSettingsStore {
  private fun database(context: Context): SQLiteDatabase {
    val path = context.getDatabasePath("decorit.db")
    check(path.exists()) { "Banco de dados não inicializado" }
    return SQLiteDatabase.openDatabase(path.path, null, SQLiteDatabase.OPEN_READWRITE).also {
      it.execSQL("CREATE TABLE IF NOT EXISTS ar_settings (layer_index INTEGER NOT NULL, field TEXT NOT NULL, value TEXT NOT NULL, value_type TEXT NOT NULL, PRIMARY KEY(layer_index, field))")
    }
  }

  fun save(context: Context, encoded: String) {
    val settings = JSONObject(encoded)
    database(context).use { db ->
      db.beginTransaction()
      try {
        db.delete("ar_settings", null, null)
        fun putObject(index: Int, obj: JSONObject) {
          val keys = obj.keys()
          while (keys.hasNext()) {
            val field = keys.next()
            val value = obj.opt(field)
            val type = when (value) { is String -> "string"; is Boolean -> "bool"; is Int, is Long -> "integer"; is Number -> "number"; else -> continue }
            db.execSQL("INSERT INTO ar_settings (layer_index, field, value, value_type) VALUES (?, ?, ?, ?)", arrayOf(index, field, value.toString(), type))
          }
        }
        putObject(-1, settings)
        val layers = settings.optJSONArray("layers") ?: JSONArray()
        for (i in 0 until layers.length()) putObject(i, layers.getJSONObject(i))
        db.setTransactionSuccessful()
      } finally { db.endTransaction() }
    }
  }

  fun load(context: Context): String {
    val settings = JSONObject()
    val layers = sortedMapOf<Int, JSONObject>()
    database(context).use { db ->
      db.rawQuery("SELECT layer_index, field, value, value_type FROM ar_settings ORDER BY layer_index, field", null).use { cursor ->
        while (cursor.moveToNext()) {
          val index = cursor.getInt(0)
          val field = cursor.getString(1)
          val raw = cursor.getString(2)
          val value: Any = when (cursor.getString(3)) { "bool" -> raw.toBooleanStrict(); "integer" -> raw.toLong(); "number" -> raw.toDouble(); else -> raw }
          (if (index < 0) settings else layers.getOrPut(index) { JSONObject() }).put(field, value)
        }
      }
    }
    if (settings.length() == 0) return ""
    val layerArray = JSONArray()
    layers.values.forEach { layerArray.put(it) }
    settings.put("layers", layerArray)
    return settings.toString()
  }
}
