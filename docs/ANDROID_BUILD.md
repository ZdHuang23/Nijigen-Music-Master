# Android 构建说明（targetSdk 36 / ARM64）

工程编辑器版本仍是 **Unity 2019.4.20f1c1**（`ProjectSettings/ProjectVersion.txt`）。本分支把 Android 播放器设置、清单和 Gradle 模板调到当前 API，并提供导出后的 Gradle 升级脚本。云端环境没有可用的 Unity 许可证，**没有打出 APK/AAB**。下面是在本机打出 ARM 包的步骤。

## 当前工程配置

| 项 | 值 |
| --- | --- |
| Unity 工程版本 | 2019.4.20f1c1（changeset `ff6a2c17d3ab`，Unity 中国版） |
| 可用的国际版编辑器 | 2019.4.20f1，changeset `6dd1c08eedfa` |
| 包名 | `com.stelledge.nijigenmusicmaster` |
| minSdk | 24 |
| targetSdk / compileSdk | 36（Player Settings 与 Gradle 占位符 `**TARGETSDKVERSION**` / `**APIVERSION**`） |
| ABI | ARMv7 + ARM64（`AndroidTargetArchitectures = 3`，即 `armeabi-v7a` 与 `arm64-v8a`） |
| 脚本后端 | IL2CPP（ARM64 必需；Mono 无法产出 arm64-v8a） |
| 产物路径（批处理成功时） | `build/android/NijigenMusicMaster.apk` |

`Assets/Plugins/Android/` 下的 `mainTemplate.gradle`、`launcherTemplate.gradle`、`baseProjectTemplate.gradle` 使用 Unity 2019.4 的 `**TOKEN**` 占位符，仓库指向 `google()` 与 `mavenCentral()`（不再使用已关闭的 jcenter）。这些模板仍声明 Android Gradle Plugin **3.4.0**，因为 2019.4 自带的 Gradle 5.1.1 跑不了 AGP 8。真正针对 API 36 的工具链升级在导出之后进行，见下文。

## 本机准备

1. 安装 **Unity 2019.4.20f1c1**。若只能装国际版 **2019.4.20f1**（`6dd1c08eedfa`），可以打开本工程；打开时若提示版本不完全一致，选择继续。
2. 安装 Android Build Support（SDK、NDK、OpenJDK）。2019.4 自带 SDK 通常没有 Platform 36，请再装：
   - Android SDK Platform **36**
   - Build-Tools **36.x**
3. 外部工具里 JDK 指向 Unity 自带的 OpenJDK（8 或 11）。不要让 2019.4 编辑器使用 JDK 17/21。
4. 磁盘上准备歌包目录（运行时用，不影响出包）。**Android 10（API 30）及以上**固定使用 `Application.persistentDataPath/MusicData`（应用专属目录，无需「所有文件访问」）。更低版本仍可读 `/storage/emulated/0/NijigenMusicMaster/MusicData` 等旧路径。

国际版编辑器：

```text
https://download.unity3d.com/download_unity/6dd1c08eedfa/LinuxEditorInstaller/Unity.tar.xz
```

中国版 changeset `ff6a2c17d3ab` 在 `download.unity3d.com` 上返回 404，需要从 Unity Hub（中国）安装。

## 编辑器里确认

**Edit → Project Settings → Player → Android**

- Package Name：`com.stelledge.nijigenmusicmaster`
- Minimum API Level：24
- Target API Level：36。若下拉列表没有 36，保持 `ProjectSettings/ProjectSettings.asset` 里的 `AndroidTargetSdkVersion: 36`，不要在界面里改回 Automatic。
- Scripting Backend：IL2CPP
- Target Architectures：ARMv7 与 ARM64 都勾选
- Publishing Settings：Custom Main / Launcher / Base Gradle Template 指向 `Assets/Plugins/Android/` 下同名文件

**File → Build Settings**

- 平台切到 Android
- 场景顺序：`gameplay` → `songselect`（已写入 `EditorBuildSettings.asset`，并提交 `Assets/Scenes/*.unity.meta`）

## 在编辑器里直接出 APK

仅当本机 SDK 与 2019.4 的 Gradle 能接受 compileSdk 36 时才会成功。多数 2019.4 环境会在 Gradle 阶段失败，这时改用下一节的导出流程。

```bash
UNITY_PATH="/path/to/Editor/Unity"   # 2019.4.20f1c1 或 2019.4.20f1

"$UNITY_PATH" -batchmode -nographics -quit \
  -projectPath "$(pwd)" \
  -executeMethod AndroidBuild.BuildApk \
  -logFile build/android/unity-build.log
```

成功时日志末尾有 `Android APK written to:`，文件为 `build/android/NijigenMusicMaster.apk`。

批处理脚本会再次写入包名、minSdk 24、targetSdk 36、IL2CPP、ARMv7+ARM64。生成 Gradle 工程后，`AndroidGradleSdkPatcher` 会把 `compileSdkVersion` / `targetSdkVersion` 数字改成 36，并写入 `android.suppressUnsupportedCompileSdk=36`。

## 导出后用现代 Gradle 出 APK / AAB（推荐）

2019.4 无法用自带 AGP 3.4.0 编译 API 36。导出 Gradle 工程，再用 AGP **8.9.1**（支持 API 36 的最低稳定版）和 Gradle **8.11.1** 编译。

1. Unity：**File → Build Settings → Export Project**，目录例如 `build/gradle-export`。不要勾选 Build App Bundle（导出工程即可）。
2. 安装 **JDK 17 或 21**，以及 Android SDK Platform 36、Build-Tools 36。设置 `ANDROID_HOME`。
3. 在仓库根目录执行：

```bash
python3 tools/upgrade_exported_android_gradle.py build/gradle-export
cd build/gradle-export
chmod +x gradlew 2>/dev/null || true
# 若导出目录没有 gradlew，先安装 Gradle 8.11.1，再执行 gradle wrapper
./gradlew :launcher:assembleRelease
./gradlew :launcher:bundleRelease
```

脚本会：

- 把 Android Gradle Plugin 换成 `8.9.1`
- 把 wrapper 指到 `gradle-8.11.1-bin.zip`
- 把 `jcenter()` 换成 `mavenCentral()`
- 把 `compileSdkVersion` / `targetSdkVersion` 设为 36
- 去掉 AGP 8 已删除的 `useProguard`
- 为 `launcher` 与 `unityLibrary` 补上 `namespace`
- 写入 `android.useAndroidX`、`android.enableJetifier`、`android.suppressUnsupportedCompileSdk=36`

APK 一般在 `launcher/build/outputs/apk/release/`。AAB 一般在 `launcher/build/outputs/bundle/release/`。未配置自定义 keystore 时，导出工程使用 Unity 的调试签名。

## 清单与权限

`Assets/Plugins/Android/AndroidManifest.xml`：

- 启动 Activity 带 `android:exported="true"`（API 31+ 必需）
- `application` 带 `android:requestLegacyExternalStorage="true"`（只对 API 29 仍有意义）
- `READ_EXTERNAL_STORAGE` 限制到 API 32
- `WRITE_EXTERNAL_STORAGE` 限制到 API 28。Player Settings 里 **Force SD Card Permission** 已关闭，避免再注入一条不带 `maxSdkVersion` 的写存储权限
- `READ_MEDIA_AUDIO`、`READ_MEDIA_IMAGES`（API 33+ 媒体权限）
- `MANAGE_EXTERNAL_STORAGE`：歌包目录在共享存储上，API 30+ 需要「所有文件访问权限」

启动时 `MusicLoader.InitMusicLoader()` 调用 `AndroidStorageAccess.RequestIfNeeded()`：API 32 及以下请求读取存储；API 30 及以上若尚未获得所有文件访问权限，会打开系统设置页。

## 校验

```bash
aapt dump badging path/to/app.apk | egrep 'package|sdkVersion|native-code'
```

期望：

- `package: name='com.stelledge.nijigenmusicmaster'`
- `sdkVersion:'24'`
- `targetSdkVersion:'36'`
- `native-code: 'armeabi-v7a' 'arm64-v8a'`（至少包含 `arm64-v8a`）

## GitHub Actions（game-ci）

工作流：`.github/workflows/build-android.yml`

- 镜像：`game-ci/unity-builder@v4`，**unityVersion 固定为 `2019.4.20f1`**（与工程文件里的 `2019.4.20f1c1` 接近；CI 不使用中国版 changeset）。
- 构建方法：`AndroidBuild.BuildApk`（输出 APK，IL2CPP，ARMv7+ARM64）。
- 成功时上传 Artifact：**`NijigenMusicMaster-Android-apk`**（路径 `build/Android/**/*.apk` 或 `build/android/**/*.apk`）。

在仓库 **Settings → Secrets and variables → Actions** 中配置（须使用你自己的 Unity 账号，不要提交到仓库）：

| Secret | 说明 |
| --- | --- |
| `UNITY_LICENSE` | Unity 许可证文件内容（`.ulf`）。按 [game.ci 激活文档](https://game.ci/docs/github/activation) 在本机生成后粘贴。 |
| `UNITY_EMAIL` | Unity ID 登录邮箱。 |
| `UNITY_PASSWORD` | Unity ID 密码。 |

未配置上述 Secret 时，工作流会在许可证步骤失败，**不会产生 APK**。Personal 许可证约每 2 周需重新激活并更新 `UNITY_LICENSE`。

触发方式：`push`/`pull_request` 到 `master`、本仓库 `cursor/**` 分支，或手动 **workflow_dispatch**。

## 云端构建尝试

在本环境实际执行过一次，没有产出 APK/AAB。

1. 下载并解压国际版编辑器 **2019.4.20f1**（changeset `6dd1c08eedfa`）：`LinuxEditorInstaller/Unity.tar.xz`（1773050516 字节）。工程记录的中国版 changeset `ff6a2c17d3ab` 在 `download.unity3d.com` 上是 404。
2. 官方 `download.html` 没有 Linux 版 Android Build Support `.tar.xz`，只有 macOS `.pkg`（329324545 字节）。已用 7-Zip 解开并放进 `Editor/Data/PlaybackEngines/AndroidPlayer`。该包不含 NDK/SDK/OpenJDK，Hub 会另外下载。
3. Ubuntu 24.04 缺少 `libgconf-2.so.4`，补装 jammy 的 `libgconf-2-4` 后编辑器可以启动。
4. 执行：

```bash
/path/to/Editor/Unity -batchmode -nographics -quit \
  -logFile /tmp/unity-license.log \
  -projectPath /path/to/Nijigen-Music-Master
```

退出码 **1**。日志要点：

- `LicensingClient` 因系统没有 ICU 语言包崩溃（`Couldn't find a valid ICU package`）
- 回退后：`Cancelling DisplayDialog: Failed to activate/update license Missing or bad username or password`

环境里没有 `UNITY_LICENSE`、序列号或 Unity 账号。没有许可证时批处理不会进入 Gradle，因此没有 `build/android/NijigenMusicMaster.apk`。请在已激活许可证的机器上按上面的导出流程构建。

## 剩余风险

- **2019.4 与 API 36**：编辑器内一键 Build 很可能失败。以「导出 + `upgrade_exported_android_gradle.py`」为准。长期建议把工程升级到 Unity 2022.3 LTS 或 Unity 6。
- **IL2CPP / NDK**：导出后的 native 库仍由 Unity 2019.4 的 NDK 编出。AGP 8 只负责打包；不要让新 NDK 重编 `libil2cpp.so`，除非接受链接错误。
- **歌包路径**：API 30+ 仅使用 `persistentDataPath/MusicData`；用户需自行把 `MusicData` 拷到该目录（可用 `adb push`）。旧版共享目录路径仅用于 API 29 及以下。
- **签名**：默认调试证书不能上架。发布前在 Player Settings 配置正式 keystore，或对导出工程配置 `signingConfigs.release`。
- **`.meta`**：仅提交 `Assets/Scenes/*.unity.meta`；其余资源仍忽略 meta，首次打开可能重新生成 GUID。
- **`READ_MEDIA_IMAGES`**：歌包封面按文件路径读取；在 scoped 目录下通常不需要该权限，但为兼容旧路径仍保留声明。
