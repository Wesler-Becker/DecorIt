package com.weslerbecker.decorit

import android.security.keystore.KeyGenParameterSpec
import android.security.keystore.KeyProperties
import android.util.Base64
import com.facebook.react.bridge.Arguments
import com.facebook.react.bridge.Promise
import com.facebook.react.bridge.ReactApplicationContext
import com.facebook.react.bridge.ReactContextBaseJavaModule
import com.facebook.react.bridge.ReactMethod
import com.facebook.react.bridge.ReadableArray
import java.security.KeyStore
import javax.crypto.Cipher
import javax.crypto.KeyGenerator
import javax.crypto.SecretKey
import javax.crypto.spec.GCMParameterSpec

class SecureFieldsModule(context: ReactApplicationContext) : ReactContextBaseJavaModule(context) {
  override fun getName() = "SecureFields"
  private val alias = "decorit.fields.aesgcm.v1"
  private val prefix = "enc:v1:"

  private fun key(create: Boolean): SecretKey {
    val store = KeyStore.getInstance("AndroidKeyStore").apply { load(null) }
    (store.getKey(alias, null) as? SecretKey)?.let { return it }
    check(create) { "Chave de criptografia ausente" }
    return KeyGenerator.getInstance(KeyProperties.KEY_ALGORITHM_AES, "AndroidKeyStore").apply {
      init(KeyGenParameterSpec.Builder(alias, KeyProperties.PURPOSE_ENCRYPT or KeyProperties.PURPOSE_DECRYPT)
        .setBlockModes(KeyProperties.BLOCK_MODE_GCM).setEncryptionPaddings(KeyProperties.ENCRYPTION_PADDING_NONE)
        .setKeySize(256).build())
    }.generateKey()
  }

  @ReactMethod
  fun transform(values: ReadableArray, encrypt: Boolean, promise: Promise) {
    try {
      val result = Arguments.createArray()
      for (i in 0 until values.size()) {
        val value = values.getString(i) ?: ""
        if (value.isEmpty() || !encrypt && !value.startsWith(prefix)) {
          result.pushString(value)
          continue
        }
        val cipher = Cipher.getInstance("AES/GCM/NoPadding")
        if (encrypt) {
          cipher.init(Cipher.ENCRYPT_MODE, key(true))
          val bytes = cipher.iv + cipher.doFinal(value.toByteArray(Charsets.UTF_8))
          result.pushString(prefix + Base64.encodeToString(bytes, Base64.NO_WRAP))
        } else {
          val bytes = Base64.decode(value.substring(prefix.length), Base64.NO_WRAP)
          require(bytes.size >= 28) { "Valor criptografado inválido" }
          cipher.init(Cipher.DECRYPT_MODE, key(false), GCMParameterSpec(128, bytes.copyOfRange(0, 12)))
          result.pushString(String(cipher.doFinal(bytes.copyOfRange(12, bytes.size)), Charsets.UTF_8))
        }
      }
      promise.resolve(result)
    } catch (error: Exception) { promise.reject("SECURE_FIELDS_FAILED", error.message, error) }
  }
}
