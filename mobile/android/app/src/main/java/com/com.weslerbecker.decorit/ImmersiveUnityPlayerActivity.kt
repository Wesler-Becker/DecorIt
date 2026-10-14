package com.weslerbecker.decorit

import android.os.Bundle
import androidx.core.view.WindowInsetsCompat
import androidx.core.view.WindowInsetsControllerCompat
import com.unity3d.player.UnityPlayerGameActivity

class ImmersiveUnityPlayerActivity : UnityPlayerGameActivity() {
  override fun onCreate(savedInstanceState: Bundle?) {
    super.onCreate(savedInstanceState)
    hideNavigationBar()
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
}
