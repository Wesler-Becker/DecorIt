package com.weslerbecker.decorit

import android.content.Intent
import android.os.Bundle
import android.util.Log
import androidx.core.view.WindowInsetsCompat
import androidx.core.view.WindowInsetsControllerCompat
import com.facebook.react.ReactActivity
import com.facebook.react.ReactActivityDelegate
import com.facebook.react.defaults.DefaultNewArchitectureEntryPoint.fabricEnabled
import com.facebook.react.defaults.DefaultReactActivityDelegate
import com.swmansion.rnscreens.fragment.restoration.RNScreensFragmentFactory

class MainActivity : ReactActivity() {

  override fun onCreate(savedInstanceState: Bundle?) {
    supportFragmentManager.fragmentFactory = RNScreensFragmentFactory()
    super.onCreate(savedInstanceState)
    hideNavigationBar()
    maybeLaunchUnitySmokeTest(intent)
  }

  override fun onResume() {
    super.onResume()
    hideNavigationBar()
  }

  override fun onWindowFocusChanged(hasFocus: Boolean) {
    super.onWindowFocusChanged(hasFocus)
    if (hasFocus) hideNavigationBar()
  }

  private fun hideNavigationBar() {
    WindowInsetsControllerCompat(window, window.decorView).apply {
      systemBarsBehavior = WindowInsetsControllerCompat.BEHAVIOR_SHOW_TRANSIENT_BARS_BY_SWIPE
      hide(WindowInsetsCompat.Type.navigationBars())
    }
  }

  override fun onNewIntent(intent: Intent) {
    super.onNewIntent(intent)
    setIntent(intent)
    maybeLaunchUnitySmokeTest(intent)
  }

  private fun maybeLaunchUnitySmokeTest(intent: Intent?) {
    if (!BuildConfig.DEBUG || intent?.getBooleanExtra("decorit_unity_smoke_test", false) != true) return

    try {
      UnityArLauncher.open(
        this,
        "smoke-client",
        "smoke-room",
        "smoke-record",
        """{"clientName":"Teste de integração","roomName":"Cômodo de teste","widthCentimeters":120,"heightCentimeters":140,"panelType":"DoublePanel"}""",
      )
      Log.i("DecorItArSmokeTest", "A Unity Activity foi solicitada pelo lançador nativo do app.")
    } catch (error: Exception) {
      Log.e("DecorItArSmokeTest", "Falha ao solicitar abertura da Unity.", error)
    }
  }

  /**
   * Returns the name of the main component registered from JavaScript. This is used to schedule
   * rendering of the component.
   */
  override fun getMainComponentName(): String = "DecorItMobile"

  /**
   * Returns the instance of the [ReactActivityDelegate]. We use [DefaultReactActivityDelegate]
   * which allows you to enable New Architecture with a single boolean flags [fabricEnabled]
   */
  override fun createReactActivityDelegate(): ReactActivityDelegate =
      DefaultReactActivityDelegate(this, mainComponentName, fabricEnabled)
}

