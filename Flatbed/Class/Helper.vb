Imports System
Imports System.Drawing
Imports System.IO
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices
Imports GTA
Imports GTA.Math
Imports GTA.Native
Imports Metadata

Module Helper

    'Config
    Public config As ScriptSettings = ScriptSettings.Load("scripts\Flatbed\Config.ini")
    Public marker As Boolean = True
    Public hookKey As Control = Control.VehicleDuck
    Public liftKey As Control = Control.VehicleSubAscend
    Public lowerKey As Control = Control.VehicleSubDescend
    Public controllerModifierKey As Control = Control.ScriptRB
    Public controllerLiftBtn As Control = Control.ScriptPadUp
    Public controllerLowerBtn As Control = Control.ScriptPadDown
    Public manualControl As Boolean = False
    Public fbVehs As New List(Of FlatbedVeh)

    'Decor
    Public modDecor As String = "inm_flatbed_installed"
    Public towVehDecor As String = "inm_flatbed_vehicle"
    Public lastFbVehDecor As String = "inm_flatbed_last"
    Public helpDecor As String = "inm_flatbed_help"
    Public persistencePauseDecor As String = "inm_persistence_pause"
    Public gHeightDecor As String = "inm_flatbed_groundheight"
    Public scoopDecor As String = "inm_flatbed_scoop_pos"

    Public PP As Ped
    Public LV As Vehicle, LF As Vehicle
    Public NV As Vehicle
    Public AC As New List(Of VehicleClass) From {VehicleClass.Commercial, VehicleClass.Compacts, VehicleClass.Coupes, VehicleClass.Cycles, VehicleClass.Emergency, VehicleClass.Industrial,
        VehicleClass.Military, VehicleClass.Motorcycles, VehicleClass.Muscle, VehicleClass.OffRoad, VehicleClass.Sedans, VehicleClass.Service, VehicleClass.Sports, VehicleClass.SportsClassics,
        VehicleClass.Super, VehicleClass.SUVs, VehicleClass.Utility, VehicleClass.Vans}
    Public LFList As New List(Of Vehicle) From {Game.Player.Character.LastFlatbed}
    Public xmlPath As String = ".\scripts\Flatbed\Vehicles\"

    <Extension>
    Public Function GetNearestFlatbed(pos As Vector3) As Vehicle
        Return LFList.OrderBy(Function(x) System.Math.Abs(x.Position.DistanceTo(pos))).First
    End Function

    <Extension>
    Public Function AttachCoords(vehicle As Vehicle) As Vector3
        Return New Vector3(0F, 1.0F, 0.1F + vehicle.GetFloat(gHeightDecor))
    End Function

    <Extension>
    Public Function IsAnyVehicleNearAttachPosition(pos As Vector3, radius As Single) As Boolean
        Return Native.Function.Call(Of Boolean)(Hash.IS_ANY_VEHICLE_NEAR_POINT, pos.X, pos.Y, pos.Z, radius)
    End Function

    <Extension>
    Public Function IsThisFlatbed3(veh As Vehicle) As Boolean
        Return fbVehs.Contains(fbVehs.Find(Function(x) x.Model = veh.Model))
    End Function

    <Extension>
    Public Function CurrentTowingVehicle(veh As Vehicle) As Vehicle
        Return New Vehicle(veh.GetInt(towVehDecor))
    End Function

    <Extension>
    Public Sub CurrentTowingVehicle(flatbed As Vehicle, veh As Vehicle)
        If veh = Nothing Then flatbed.SetInt(towVehDecor, 0) Else flatbed.SetInt(towVehDecor, veh.Handle)
    End Sub

    <Extension>
    Public Function GroundHeight(ent As Entity) As Single
        Return ent.HeightAboveGround
    End Function

    <Extension>
    Public Function IsFlatbedDropped(veh As Vehicle) As Boolean
        Dim result As Boolean = False
        Select Case veh.GetBoneCoord("engine").DistanceTo(veh.AttachDummyPos)
            Case 7.0F To 9.5F
                result = False
            Case 9.5F To 13.0F
                result = True
        End Select
        Return result
    End Function

    <Extension>
    Public Function AttachPosition(veh As Vehicle) As Vector3
        Return veh.AttachDummyPos - (veh.ForwardVector * 7)
    End Function

    <Extension>
    Public Function DetachPosition(veh As Vehicle) As Vector3
        Return veh.AttachDummyPos - (veh.ForwardVector * 10)
    End Function

    <Extension>
    Public Sub DrawMarkerTick(veh As Vehicle)
        If veh.IsFlatbedDropped AndAlso veh.CurrentTowingVehicle.Handle = 0 Then
            Dim pos As New Vector3(veh.AttachPosition.X, veh.AttachPosition.Y, veh.AttachPosition.Z - 1.0F)
            World.DrawMarker(MarkerType.VerticalCylinder, pos, Vector3.Zero, Vector3.Zero, New Vector3(2.0F, 2.0F, 2.0F), Color.FromArgb(100, Color.GreenYellow))
        End If
        If veh.IsControlOutside AndAlso Not PP.IsInVehicle(veh) Then
            If veh.HasBone(veh.ControlDummyBone) Then
                Dim pos As New Vector3(veh.ControlDummyPos.X, veh.ControlDummyPos.Y, veh.ControlDummyPos.Z - 1.0F)
                World.DrawMarker(MarkerType.VerticalCylinder, pos, Vector3.Zero, Vector3.Zero, New Vector3(1.0F, 1.0F, 0.5F), Color.FromArgb(100, Color.WhiteSmoke))
            End If
            If veh.HasBone(veh.ControlDummy2Bone) Then
                Dim pos As New Vector3(veh.ControlDummy2Pos.X, veh.ControlDummy2Pos.Y, veh.ControlDummy2Pos.Z - 1.0F)
                World.DrawMarker(MarkerType.VerticalCylinder, pos, Vector3.Zero, Vector3.Zero, New Vector3(1.0F, 1.0F, 0.5F), Color.FromArgb(100, Color.WhiteSmoke))
            End If
        End If
    End Sub

    <Extension>
    Public Sub TurnOnIndicators(veh As Vehicle)
        veh.LeftIndicatorLightOn = True
        veh.RightIndicatorLightOn = True
        If Not veh.EngineRunning Then veh.EngineRunning = True
    End Sub

    <Extension>
    Public Sub TurnOffIndicators(veh As Vehicle)
        veh.LeftIndicatorLightOn = False
        veh.RightIndicatorLightOn = False
    End Sub

    <Extension>
    Public Sub AttachToFix(entity1 As Entity, entity2 As Entity, boneindex As Integer, position As Vector3, rotation As Vector3)
        Dim fixedRot As Boolean = True
        Dim vertexIndex As Integer = 2
        Dim isPed As Boolean = False
        Dim col As Boolean = True
        Dim useSoftPinning As Boolean = True
        Native.Function.Call(Hash.ATTACH_ENTITY_TO_ENTITY, entity1.Handle, entity2.Handle, boneindex, position.X, position.Y, position.Z, rotation.X, rotation.Y, rotation.Z,
                             False, useSoftPinning, col, isPed, vertexIndex, fixedRot)
    End Sub

    <Extension>
    Public Sub AttachToPhysically(entity1 As Entity, entity2 As Entity, boneindex1 As Integer, boneindex2 As Integer, position1 As Vector3, rotation As Vector3)
        Native.Function.Call(Hash.ATTACH_ENTITY_TO_ENTITY_PHYSICALLY, entity1.Handle, entity2.Handle,
             boneindex1, boneindex2, position1.X, position1.Y, position1.Z, 0.0, 0.0, 0.0,
             rotation.X, rotation.Y, rotation.Z, 5000.0, True, False, True, True, 2)
        '                                       bForce   fRot   unk   col notelpot alwys 2
    End Sub

    <Extension>
    Public Sub DetachToFix(carToDetach As Vehicle, facingBackwards As Boolean)
        Dim attachedCar As Vehicle = carToDetach.GetEntityAttachedTo
        Dim p2 As New Vector3(carToDetach.AttachCoords().X, carToDetach.AttachCoords().Y, carToDetach.AttachCoords().Z + 0.3F)
        Native.Function.Call(Hash.DETACH_ENTITY, carToDetach, True, True)
        Script.Wait(10)
        If facingBackwards Then carToDetach.AttachToFix(attachedCar, attachedCar.AttachDummyIndex, p2, New Vector3(0F, 0F, 180.0F)) Else carToDetach.AttachToFix(attachedCar, attachedCar.AttachDummyIndex, p2, Vector3.Zero)
        Script.Wait(10)
        Native.Function.Call(Hash.DETACH_ENTITY, carToDetach, True, True)
        Script.Wait(10)
        carToDetach.AttachToPhysically(attachedCar, attachedCar.AttachDummyIndex, 0, p2, Vector3.Zero)
        Script.Wait(10)
        Native.Function.Call(Hash.DETACH_ENTITY, carToDetach, True, True)
    End Sub

    Public Sub DisplayHelpTextThisFrame(helpText As String, Optional Shape As Integer = -1)
        Native.Function.Call(Hash._SET_TEXT_COMPONENT_FORMAT, "CELL_EMAIL_BCON")
        Const maxStringLength As Integer = 99

        Dim i As Integer = 0
        While i < helpText.Length
            Native.Function.Call(Hash._0x6C188BE134E074AA, helpText.Substring(i, System.Math.Min(maxStringLength, helpText.Length - i)))
            i += maxStringLength
        End While
        Native.Function.Call(Hash._DISPLAY_HELP_TEXT_FROM_STRING_LABEL, 0, 0, 1, Shape)
    End Sub

    Public Function Cheating(Cheat As String) As Boolean
        Return Native.Function.Call(Of Boolean)(Hash._0x557E43C447E700A8, Game.GenerateHash(Cheat))
    End Function

    <Extension>
    Public Function LastFlatbed(ped As Ped) As Vehicle
        Return New Vehicle(ped.GetInt(lastFbVehDecor))
    End Function

    <Extension>
    Public Sub LastFlatbed(ped As Ped, veh As Vehicle)
        ped.SetInt(lastFbVehDecor, veh.Handle)
    End Sub

    <Extension>
    Public Sub PushVehicleBack(veh As Vehicle)
        Native.Function.Call(Hash.APPLY_FORCE_TO_ENTITY, veh, 3, 0.0, -0.3F, 0.0, 0.0, 0.0, 0.0, 0, True, True, True, True, True)
    End Sub

    <Extension>
    Public Sub PushVehicleForward(veh As Vehicle)
        Native.Function.Call(Hash.APPLY_FORCE_TO_ENTITY, veh, 3, 0.0, 0.3F, 0.0, 0.0, 0.0, 0.0, 0, True, True, True, True, True)
    End Sub

    Public Sub LoadSettings()
        CreateConfig()
        marker = config.GetValue(Of Boolean)("SETTING", "MARKER", True)
        manualControl = config.GetValue(Of Boolean)("SETTING", "MANUALCONTROL", False)
        hookKey = config.GetValue(Of Control)("CONTROL", "HOOKKEY", Control.VehicleDuck)
        liftKey = config.GetValue(Of Control)("CONTROL", "LIFTKEY", Control.VehicleSubAscend)
        lowerKey = config.GetValue(Of Control)("CONTROL", "LOWERKEY", Control.VehicleSubDescend)
        controllerModifierKey = config.GetValue(Of Control)("JOYSTICK", "MODIFIER", Control.ScriptRB)
        controllerLiftBtn = config.GetValue(Of Control)("JOYSTICK", "LIFTBTN", Control.ScriptPadUp)
        controllerLowerBtn = config.GetValue(Of Control)("JOYSTICK", "LOWERBTN", Control.ScriptPadDown)
    End Sub

    Private Sub CreateConfig()
        If Not File.Exists("scripts\Flatbed\Config.ini") Then
            config.SetValue(Of Boolean)("SETTING", "MARKER", True)
            config.SetValue(Of Boolean)("SETTING", "MANUALCONTROL", False)
            config.SetValue(Of Control)("CONTROL", "HOOKKEY", Control.VehicleDuck)
            config.SetValue(Of Control)("CONTROL", "LIFTKEY", Control.VehicleSubAscend)
            config.SetValue(Of Control)("CONTROL", "LOWERKEY", Control.VehicleSubDescend)
            config.SetValue(Of Control)("JOYSTICK", "MODIFIER", Control.ScriptRB)
            config.SetValue(Of Control)("JOYSTICK", "LIFTBTN", Control.ScriptPadUp)
            config.SetValue(Of Control)("JOYSTICK", "LOWERBTN", Control.ScriptPadDown)
            config.Save()
        End If
    End Sub

    Public Sub RegisterDecor(d As String, t As Decor.eDecorType)
        If Not Decor.Registered(d, t) Then
            Decor.Unlock()
            Decor.Register(d, t)
            Decor.Lock()
        End If
    End Sub

    <Extension>
    Public Function GetButtonIcon(control As GTA.Control) As String
        Return String.Format("~{0}~", [Enum].GetName(GetType(ControlButtonIcon), control))
    End Function

    Enum ControlButtonIcon
        INPUT_NEXT_CAMERA
        INPUT_LOOK_LR
        INPUT_LOOK_UD
        INPUT_LOOK_UP_ONLY
        INPUT_LOOK_DOWN_ONLY
        INPUT_LOOK_LEFT_ONLY
        INPUT_LOOK_RIGHT_ONLY
        INPUT_CINEMATIC_SLOWMO
        INPUT_SCRIPTED_FLY_UD
        INPUT_SCRIPTED_FLY_LR
        INPUT_SCRIPTED_FLY_ZUP
        INPUT_SCRIPTED_FLY_ZDOWN
        INPUT_WEAPON_WHEEL_UD
        INPUT_WEAPON_WHEEL_LR
        INPUT_WEAPON_WHEEL_NEXT
        INPUT_WEAPON_WHEEL_PREV
        INPUT_SELECT_NEXT_WEAPON
        INPUT_SELECT_PREV_WEAPON
        INPUT_SKIP_CUTSCENE
        INPUT_CHARACTER_WHEEL
        INPUT_MULTIPLAYER_INFO
        INPUT_SPRINT
        INPUT_JUMP
        INPUT_ENTER
        INPUT_ATTACK
        INPUT_AIM
        INPUT_LOOK_BEHIND
        INPUT_PHONE
        INPUT_SPECIAL_ABILITY
        INPUT_SPECIAL_ABILITY_SECONDARY
        INPUT_MOVE_LR
        INPUT_MOVE_UD
        INPUT_MOVE_UP_ONLY
        INPUT_MOVE_DOWN_ONLY
        INPUT_MOVE_LEFT_ONLY
        INPUT_MOVE_RIGHT_ONLY
        INPUT_DUCK
        INPUT_SELECT_WEAPON
        INPUT_PICKUP
        INPUT_SNIPER_ZOOM
        INPUT_SNIPER_ZOOM_IN_ONLY
        INPUT_SNIPER_ZOOM_OUT_ONLY
        INPUT_SNIPER_ZOOM_IN_SECONDARY
        INPUT_SNIPER_ZOOM_OUT_SECONDARY
        INPUT_COVER
        INPUT_RELOAD
        INPUT_TALK
        INPUT_DETONATE
        INPUT_HUD_SPECIAL
        INPUT_ARREST
        INPUT_ACCURATE_AIM
        INPUT_CONTEXT
        INPUT_CONTEXT_SECONDARY
        INPUT_WEAPON_SPECIAL
        INPUT_WEAPON_SPECIAL_TWO
        INPUT_DIVE
        INPUT_DROP_WEAPON
        INPUT_DROP_AMMO
        INPUT_THROW_GRENADE
        INPUT_VEH_MOVE_LR
        INPUT_VEH_MOVE_UD
        INPUT_VEH_MOVE_UP_ONLY
        INPUT_VEH_MOVE_DOWN_ONLY
        INPUT_VEH_MOVE_LEFT_ONLY
        INPUT_VEH_MOVE_RIGHT_ONLY
        INPUT_VEH_SPECIAL
        INPUT_VEH_GUN_LR
        INPUT_VEH_GUN_UD
        INPUT_VEH_AIM
        INPUT_VEH_ATTACK
        INPUT_VEH_ATTACK2
        INPUT_VEH_ACCELERATE
        INPUT_VEH_BRAKE
        INPUT_VEH_DUCK
        INPUT_VEH_HEADLIGHT
        INPUT_VEH_EXIT
        INPUT_VEH_HANDBRAKE
        INPUT_VEH_HOTWIRE_LEFT
        INPUT_VEH_HOTWIRE_RIGHT
        INPUT_VEH_LOOK_BEHIND
        INPUT_VEH_CIN_CAM
        INPUT_VEH_NEXT_RADIO
        INPUT_VEH_PREV_RADIO
        INPUT_VEH_NEXT_RADIO_TRACK
        INPUT_VEH_PREV_RADIO_TRACK
        INPUT_VEH_RADIO_WHEEL
        INPUT_VEH_HORN
        INPUT_VEH_FLY_THROTTLE_UP
        INPUT_VEH_FLY_THROTTLE_DOWN
        INPUT_VEH_FLY_YAW_LEFT
        INPUT_VEH_FLY_YAW_RIGHT
        INPUT_VEH_PASSENGER_AIM
        INPUT_VEH_PASSENGER_ATTACK
        INPUT_VEH_SPECIAL_ABILITY_FRANKLIN
        INPUT_VEH_STUNT_UD
        INPUT_VEH_CINEMATIC_UD
        INPUT_VEH_CINEMATIC_UP_ONLY
        INPUT_VEH_CINEMATIC_DOWN_ONLY
        INPUT_VEH_CINEMATIC_LR
        INPUT_VEH_SELECT_NEXT_WEAPON
        INPUT_VEH_SELECT_PREV_WEAPON
        INPUT_VEH_ROOF
        INPUT_VEH_JUMP
        INPUT_VEH_GRAPPLING_HOOK
        INPUT_VEH_SHUFFLE
        INPUT_VEH_DROP_PROJECTILE
        INPUT_VEH_MOUSE_CONTROL_OVERRIDE
        INPUT_VEH_FLY_ROLL_LR
        INPUT_VEH_FLY_ROLL_LEFT_ONLY
        INPUT_VEH_FLY_ROLL_RIGHT_ONLY
        INPUT_VEH_FLY_PITCH_UD
        INPUT_VEH_FLY_PITCH_UP_ONLY
        INPUT_VEH_FLY_PITCH_DOWN_ONLY
        INPUT_VEH_FLY_UNDERCARRIAGE
        INPUT_VEH_FLY_ATTACK
        INPUT_VEH_FLY_SELECT_NEXT_WEAPON
        INPUT_VEH_FLY_SELECT_PREV_WEAPON
        INPUT_VEH_FLY_SELECT_TARGET_LEFT
        INPUT_VEH_FLY_SELECT_TARGET_RIGHT
        INPUT_VEH_FLY_VERTICAL_FLIGHT_MODE
        INPUT_VEH_FLY_DUCK
        INPUT_VEH_FLY_ATTACK_CAMERA
        INPUT_VEH_FLY_MOUSE_CONTROL_OVERRIDE
        INPUT_VEH_SUB_TURN_LR
        INPUT_VEH_SUB_TURN_LEFT_ONLY
        INPUT_VEH_SUB_TURN_RIGHT_ONLY
        INPUT_VEH_SUB_PITCH_UD
        INPUT_VEH_SUB_PITCH_UP_ONLY
        INPUT_VEH_SUB_PITCH_DOWN_ONLY
        INPUT_VEH_SUB_THROTTLE_UP
        INPUT_VEH_SUB_THROTTLE_DOWN
        INPUT_VEH_SUB_ASCEND
        INPUT_VEH_SUB_DESCEND
        INPUT_VEH_SUB_TURN_HARD_LEFT
        INPUT_VEH_SUB_TURN_HARD_RIGHT
        INPUT_VEH_SUB_MOUSE_CONTROL_OVERRIDE
        INPUT_VEH_PUSHBIKE_PEDAL
        INPUT_VEH_PUSHBIKE_SPRINT
        INPUT_VEH_PUSHBIKE_FRONT_BRAKE
        INPUT_VEH_PUSHBIKE_REAR_BRAKE
        INPUT_MELEE_ATTACK_LIGHT
        INPUT_MELEE_ATTACK_HEAVY
        INPUT_MELEE_ATTACK_ALTERNATE
        INPUT_MELEE_BLOCK
        INPUT_PARACHUTE_DEPLOY
        INPUT_PARACHUTE_DETACH
        INPUT_PARACHUTE_TURN_LR
        INPUT_PARACHUTE_TURN_LEFT_ONLY
        INPUT_PARACHUTE_TURN_RIGHT_ONLY
        INPUT_PARACHUTE_PITCH_UD
        INPUT_PARACHUTE_PITCH_UP_ONLY
        INPUT_PARACHUTE_PITCH_DOWN_ONLY
        INPUT_PARACHUTE_BRAKE_LEFT
        INPUT_PARACHUTE_BRAKE_RIGHT
        INPUT_PARACHUTE_SMOKE
        INPUT_PARACHUTE_PRECISION_LANDING
        INPUT_MAP
        INPUT_SELECT_WEAPON_UNARMED
        INPUT_SELECT_WEAPON_MELEE
        INPUT_SELECT_WEAPON_HANDGUN
        INPUT_SELECT_WEAPON_SHOTGUN
        INPUT_SELECT_WEAPON_SMG
        INPUT_SELECT_WEAPON_AUTO_RIFLE
        INPUT_SELECT_WEAPON_SNIPER
        INPUT_SELECT_WEAPON_HEAVY
        INPUT_SELECT_WEAPON_SPECIAL
        INPUT_SELECT_CHARACTER_MICHAEL
        INPUT_SELECT_CHARACTER_FRANKLIN
        INPUT_SELECT_CHARACTER_TREVOR
        INPUT_SELECT_CHARACTER_MULTIPLAYER
        INPUT_SAVE_REPLAY_CLIP
        INPUT_SPECIAL_ABILITY_PC
        INPUT_CELLPHONE_UP
        INPUT_CELLPHONE_DOWN
        INPUT_CELLPHONE_LEFT
        INPUT_CELLPHONE_RIGHT
        INPUT_CELLPHONE_SELECT
        INPUT_CELLPHONE_CANCEL
        INPUT_CELLPHONE_OPTION
        INPUT_CELLPHONE_EXTRA_OPTION
        INPUT_CELLPHONE_SCROLL_FORWARD
        INPUT_CELLPHONE_SCROLL_BACKWARD
        INPUT_CELLPHONE_CAMERA_FOCUS_LOCK
        INPUT_CELLPHONE_CAMERA_GRID
        INPUT_CELLPHONE_CAMERA_SELFIE
        INPUT_CELLPHONE_CAMERA_DOF
        INPUT_CELLPHONE_CAMERA_EXPRESSION
        INPUT_FRONTEND_DOWN
        INPUT_FRONTEND_UP
        INPUT_FRONTEND_LEFT
        INPUT_FRONTEND_RIGHT
        INPUT_FRONTEND_RDOWN
        INPUT_FRONTEND_RUP
        INPUT_FRONTEND_RLEFT
        INPUT_FRONTEND_RRIGHT
        INPUT_FRONTEND_AXIS_X
        INPUT_FRONTEND_AXIS_Y
        INPUT_FRONTEND_RIGHT_AXIS_X
        INPUT_FRONTEND_RIGHT_AXIS_Y
        INPUT_FRONTEND_PAUSE
        INPUT_FRONTEND_PAUSE_ALTERNATE
        INPUT_FRONTEND_ACCEPT
        INPUT_FRONTEND_CANCEL
        INPUT_FRONTEND_X
        INPUT_FRONTEND_Y
        INPUT_FRONTEND_LB
        INPUT_FRONTEND_RB
        INPUT_FRONTEND_LT
        INPUT_FRONTEND_RT
        INPUT_FRONTEND_LS
        INPUT_FRONTEND_RS
        INPUT_FRONTEND_LEADERBOARD
        INPUT_FRONTEND_SOCIAL_CLUB
        INPUT_FRONTEND_SOCIAL_CLUB_SECONDARY
        INPUT_FRONTEND_DELETE
        INPUT_FRONTEND_ENDSCREEN_ACCEPT
        INPUT_FRONTEND_ENDSCREEN_EXPAND
        INPUT_FRONTEND_SELECT
        INPUT_SCRIPT_LEFT_AXIS_X
        INPUT_SCRIPT_LEFT_AXIS_Y
        INPUT_SCRIPT_RIGHT_AXIS_X
        INPUT_SCRIPT_RIGHT_AXIS_Y
        INPUT_SCRIPT_RUP
        INPUT_SCRIPT_RDOWN
        INPUT_SCRIPT_RLEFT
        INPUT_SCRIPT_RRIGHT
        INPUT_SCRIPT_LB
        INPUT_SCRIPT_RB
        INPUT_SCRIPT_LT
        INPUT_SCRIPT_RT
        INPUT_SCRIPT_LS
        INPUT_SCRIPT_RS
        INPUT_SCRIPT_PAD_UP
        INPUT_SCRIPT_PAD_DOWN
        INPUT_SCRIPT_PAD_LEFT
        INPUT_SCRIPT_PAD_RIGHT
        INPUT_SCRIPT_SELECT
        INPUT_CURSOR_ACCEPT
        INPUT_CURSOR_CANCEL
        INPUT_CURSOR_X
        INPUT_CURSOR_Y
        INPUT_CURSOR_SCROLL_UP
        INPUT_CURSOR_SCROLL_DOWN
        INPUT_ENTER_CHEAT_CODE
        INPUT_INTERACTION_MENU
        INPUT_MP_TEXT_CHAT_ALL
        INPUT_MP_TEXT_CHAT_TEAM
        INPUT_MP_TEXT_CHAT_FRIENDS
        INPUT_MP_TEXT_CHAT_CREW
        INPUT_PUSH_TO_TALK
        INPUT_CREATOR_LS
        INPUT_CREATOR_RS
        INPUT_CREATOR_LT
        INPUT_CREATOR_RT
        INPUT_CREATOR_MENU_TOGGLE
        INPUT_CREATOR_ACCEPT
        INPUT_CREATOR_DELETE
        INPUT_ATTACK2
        INPUT_RAPPEL_JUMP
        INPUT_RAPPEL_LONG_JUMP
        INPUT_RAPPEL_SMASH_WINDOW
        INPUT_PREV_WEAPON
        INPUT_NEXT_WEAPON
        INPUT_MELEE_ATTACK1
        INPUT_MELEE_ATTACK2
        INPUT_WHISTLE
        INPUT_MOVE_LEFT
        INPUT_MOVE_RIGHT
        INPUT_MOVE_UP
        INPUT_MOVE_DOWN
        INPUT_LOOK_LEFT
        INPUT_LOOK_RIGHT
        INPUT_LOOK_UP
        INPUT_LOOK_DOWN
        INPUT_SNIPER_ZOOM_IN
        INPUT_SNIPER_ZOOM_OUT
        INPUT_SNIPER_ZOOM_IN_ALTERNATE
        INPUT_SNIPER_ZOOM_OUT_ALTERNATE
        INPUT_VEH_MOVE_LEFT
        INPUT_VEH_MOVE_RIGHT
        INPUT_VEH_MOVE_UP
        INPUT_VEH_MOVE_DOWN
        INPUT_VEH_GUN_LEFT
        INPUT_VEH_GUN_RIGHT
        INPUT_VEH_GUN_UP
        INPUT_VEH_GUN_DOWN
        INPUT_VEH_LOOK_LEFT
        INPUT_VEH_LOOK_RIGHT
        INPUT_REPLAY_START_STOP_RECORDING
        INPUT_REPLAY_START_STOP_RECORDING_SECONDARY
        INPUT_SCALED_LOOK_LR
        INPUT_SCALED_LOOK_UD
        INPUT_SCALED_LOOK_UP_ONLY
        INPUT_SCALED_LOOK_DOWN_ONLY
        INPUT_SCALED_LOOK_LEFT_ONLY
        INPUT_SCALED_LOOK_RIGHT_ONLY
        INPUT_REPLAY_MARKER_DELETE
        INPUT_REPLAY_CLIP_DELETE
        INPUT_REPLAY_PAUSE
        INPUT_REPLAY_REWIND
        INPUT_REPLAY_FFWD
        INPUT_REPLAY_NEWMARKER
        INPUT_REPLAY_RECORD
        INPUT_REPLAY_SCREENSHOT
        INPUT_REPLAY_HIDEHUD
        INPUT_REPLAY_STARTPOINT
        INPUT_REPLAY_ENDPOINT
        INPUT_REPLAY_ADVANCE
        INPUT_REPLAY_BACK
        INPUT_REPLAY_TOOLS
        INPUT_REPLAY_RESTART
        INPUT_REPLAY_SHOWHOTKEY
        INPUT_REPLAY_CYCLEMARKERLEFT
        INPUT_REPLAY_CYCLEMARKERRIGHT
        INPUT_REPLAY_FOVINCREASE
        INPUT_REPLAY_FOVDECREASE
        INPUT_REPLAY_CAMERAUP
        INPUT_REPLAY_CAMERADOWN
        INPUT_REPLAY_SAVE
        INPUT_REPLAY_TOGGLETIME
        INPUT_REPLAY_TOGGLETIPS
        INPUT_REPLAY_PREVIEW
        INPUT_REPLAY_TOGGLE_TIMELINE
        INPUT_REPLAY_TIMELINE_PICKUP_CLIP
        INPUT_REPLAY_TIMELINE_DUPLICATE_CLIP
        INPUT_REPLAY_TIMELINE_PLACE_CLIP
        INPUT_REPLAY_CTRL
        INPUT_REPLAY_TIMELINE_SAVE
        INPUT_REPLAY_PREVIEW_AUDIO
        INPUT_VEH_DRIVE_LOOK
        INPUT_VEH_DRIVE_LOOK2
        INPUT_VEH_FLY_ATTACK2
        INPUT_RADIO_WHEEL_UD
        INPUT_RADIO_WHEEL_LR
        INPUT_VEH_SLOWMO_UD
        INPUT_VEH_SLOWMO_UP_ONLY
        INPUT_VEH_SLOWMO_DOWN_ONLY
        INPUT_MAP_POI
        INPUT_REPLAY_SNAPMATIC_PHOTO
        INPUT_VEH_CAR_JUMP
        INPUT_VEH_ROCKET_BOOST
        INPUT_VEH_PARACHUTE
        INPUT_VEH_BIKE_WINGS
    End Enum

    <Extension>
    Public Function Make(vehicle As Vehicle) As String
        Return Game.GetGXTEntry(vehicle.Model.Hash.GetVehicleMakeName)
    End Function

    <Extension>
    Public Function FullName(vehicle As Vehicle) As String
        Dim make As String = vehicle.Make
        Dim name As String = vehicle.FriendlyName
        Dim full As String = $"{make} {name}"
        If make = "NULL" Then full = name
        Return full
    End Function

    <Extension>
    Public Sub StartWinding(rope As Rope)
        Native.Function.Call(Hash.START_ROPE_WINDING, rope.Handle)
    End Sub

    <Extension>
    Public Sub StopWinding(rope As Rope)
        Native.Function.Call(Hash.STOP_ROPE_WINDING, rope.Handle)
    End Sub

    <Extension>
    Public Sub StartUnWinding(rope As Rope)
        Native.Function.Call(Hash.START_ROPE_UNWINDING_FRONT, rope.Handle)
    End Sub

    <Extension>
    Public Sub StopUnWinding(rope As Rope)
        Native.Function.Call(Hash.STOP_ROPE_UNWINDING_FRONT, rope.Handle)
    End Sub

    <Extension>
    Public Function GetRopeHook(veh As Vehicle) As Vector3
        If veh.HasBone("neon_f") Then
            Return veh.GetBoneCoord("neon_f")
        Else
            If veh.HasBone("bumper_f") Then
                Return veh.GetBoneCoord("bumper_f")
            Else
                If veh.HasBone("engine") Then
                    Return veh.GetBoneCoord("engine")
                Else
                    Return veh.Position + veh.ForwardVector
                End If
            End If
        End If
    End Function

    <Extension>
    Public Function GetRopeHookRear(veh As Vehicle) As Vector3
        If veh.HasBone("neon_b") Then
            Return veh.GetBoneCoord("neon_b")
        Else
            If veh.HasBone("bumper_r") Then
                Return veh.GetBoneCoord("bumper_r")
            Else
                If veh.HasBone("trunk") Then
                    Return veh.GetBoneCoord("trunk")
                Else
                    Return veh.Position - (veh.ForwardVector * 2)
                End If
            End If
        End If
    End Function

    <Extension>
    Public Function IsVehicleFacingFlatbed(veh As Vehicle, fb As Vehicle) As Boolean
        Dim angle As Single = 90
        Return Vector3.Angle(veh.ForwardVector, fb.Position - veh.Position) < angle
    End Function

    <Extension>
    Public Sub DropBed(veh As Vehicle)
        If veh.IsAlive Then
            If Not veh.EngineRunning Then veh.EngineRunning = True
            veh.LeftIndicatorLightOn = True
            veh.RightIndicatorLightOn = True
            Dim soundId As Integer = Audio.PlaySoundAt(veh, "Garage_Open", "CAR_STEAL_2_SOUNDSET")
            Script.Wait(500)
            Dim closeFloat As Single = 0.03F
            Dim openFloat As Single = 0.26F

            Select Case veh.GetBoneCoord("engine").DistanceTo(veh.AttachDummyPos)
                Case 6.0F To 9.0F
                    Dim initPos As Single = closeFloat
                    Do Until initPos >= openFloat
                        veh.ActivePhysics
                        initPos += 0.0006F
                        Native.Function.Call(Hash._0xF8EBCCC96ADB9FB7, veh, initPos, False)
                        Game.DisableControlThisFrame(0, Control.VehicleMoveUpDown)
                        If veh.FlatbedExtraDoorEnable Then veh.SetDoorAngle(veh.FlatbedExtraDoor, (initPos + veh.FlatbedExtraDoorAngleAdjustment))
                        Script.Wait(1)
                        veh.SetFloat(scoopDecor, initPos)
                    Loop
                    Native.Function.Call(Hash._0xF8EBCCC96ADB9FB7, veh, openFloat, False)
                Case 9.1F To 13.0F
                    Dim initPos As Single = openFloat
                    Do Until initPos <= closeFloat
                        veh.ActivePhysics
                        initPos -= 0.0006F
                        Native.Function.Call(Hash._0xF8EBCCC96ADB9FB7, veh, initPos, False)
                        Game.DisableControlThisFrame(0, Control.VehicleMoveUpDown)
                        If veh.FlatbedExtraDoorEnable Then veh.SetDoorAngle(veh.FlatbedExtraDoor, (initPos + veh.FlatbedExtraDoorAngleAdjustment))
                        Script.Wait(1)
                        veh.SetFloat(scoopDecor, initPos)
                    Loop
                    Native.Function.Call(Hash._0xF8EBCCC96ADB9FB7, veh, closeFloat, False)
            End Select

            If veh.FlatbedExtraDoorEnable Then
                If veh.GetFloat(scoopDecor) <= closeFloat Then veh.CloseDoor(veh.FlatbedExtraDoor, False)
            End If
            Audio.StopSound(soundId)
        End If
    End Sub

    <Extension>
    Public Sub DropBedManually(veh As Vehicle, isLift As Boolean)
        If veh.IsAlive Then
            If Not veh.EngineRunning Then veh.EngineRunning = True
            veh.LeftIndicatorLightOn = True
            veh.RightIndicatorLightOn = True
            Dim closeFloat As Single = 0.03F
            Dim openFloat As Single = 0.26F
            Dim scoopFloat As Single = veh.GetFloat(scoopDecor)

            Select Case isLift
                Case True
                    veh.ActivePhysics
                    scoopFloat -= 0.0003F
                    If scoopFloat <= closeFloat Then scoopFloat = closeFloat
                    Native.Function.Call(Hash._0xF8EBCCC96ADB9FB7, veh, scoopFloat, False)
                    If veh.FlatbedExtraDoorEnable Then
                        veh.SetDoorAngle(veh.FlatbedExtraDoor, scoopFloat + veh.FlatbedExtraDoorAngleAdjustment)
                        If scoopFloat <= closeFloat Then veh.CloseDoor(veh.FlatbedExtraDoor, False)
                    End If
                    veh.SetFloat(scoopDecor, scoopFloat)
                Case False
                    veh.ActivePhysics
                    scoopFloat += 0.0003F
                    If scoopFloat >= openFloat Then scoopFloat = openFloat
                    Native.Function.Call(Hash._0xF8EBCCC96ADB9FB7, veh, scoopFloat, False)
                    If veh.FlatbedExtraDoorEnable Then veh.SetDoorAngle(veh.FlatbedExtraDoor, scoopFloat + veh.FlatbedExtraDoorAngleAdjustment)
                    veh.SetFloat(scoopDecor, scoopFloat)
            End Select
        End If
    End Sub

    <Extension>
    Public Sub SetDoorAngle(veh As Vehicle, door As VehicleDoor, val As Single)
        Native.Function.Call(Hash.SET_VEHICLE_DOOR_CONTROL, veh.Handle, door, 2, val)
    End Sub

    Public Function GetLangEntry(lang As String) As String
        Dim result As String = ReadCfgValue($"{Game.Language.ToString}_{lang}", ".\scripts\Flatbed\Lang.cfg")
        Dim real_result As String = True
        If result = Nothing Then
            real_result = "NULL"
        Else
            real_result = result
        End If
        Return real_result
    End Function

    <Extension>
    Public Function FlatbedExtraDoorAngleAdjustment(veh As Vehicle) As Single
        Return fbVehs.Find(Function(x) x.Model = veh.Model).ExtraDoorAngleAdjustment
    End Function

    <Extension>
    Public Function FlatbedExtraDoorEnable(veh As Vehicle) As Boolean
        Return fbVehs.Find(Function(x) x.Model = veh.Model).EnableExtraDoor
    End Function

    <Extension>
    Public Function FlatbedExtraDoor(veh As Vehicle) As VehicleDoor
        Return fbVehs.Find(Function(x) x.Model = veh.Model).ExtraDoorMove
    End Function

    <Extension>
    Public Function AttachDummyPos(veh As Vehicle) As Vector3
        Return veh.GetBoneCoord(fbVehs.Find(Function(x) x.Model = veh.Model).AttachDummy)
    End Function

    <Extension>
    Public Function WinchDummyPos(veh As Vehicle) As Vector3
        Return veh.GetBoneCoord(fbVehs.Find(Function(x) x.Model = veh.Model).WinchDummy)
    End Function

    <Extension>
    Public Function ControlDummyPos(veh As Vehicle) As Vector3
        Return veh.GetBoneCoord(fbVehs.Find(Function(x) x.Model = veh.Model).ControlDummy)
    End Function

    <Extension>
    Public Function ControlDummy2Pos(veh As Vehicle) As Vector3
        Return veh.GetBoneCoord(fbVehs.Find(Function(x) x.Model = veh.Model).ControlDummy2)
    End Function

    <Extension>
    Public Function AttachDummyIndex(veh As Vehicle) As Integer
        Return veh.GetBoneIndex(fbVehs.Find(Function(x) x.Model = veh.Model).AttachDummy)
    End Function

    <Extension>
    Public Function WinchDummyIndex(veh As Vehicle) As Integer
        Return veh.GetBoneIndex(fbVehs.Find(Function(x) x.Model = veh.Model).WinchDummy)
    End Function

    <Extension>
    Public Function ControlDummyIndex(veh As Vehicle) As Integer
        Return veh.GetBoneIndex(fbVehs.Find(Function(x) x.Model = veh.Model).ControlDummy)
    End Function

    <Extension>
    Public Function ControlDummy2Index(veh As Vehicle) As Integer
        Return veh.GetBoneIndex(fbVehs.Find(Function(x) x.Model = veh.Model).ControlDummy2)
    End Function

    <Extension>
    Public Function ControlDummyBone(veh As Vehicle) As String
        Return fbVehs.Find(Function(x) x.Model = veh.Model).ControlDummy
    End Function

    <Extension>
    Public Function ControlDummy2Bone(veh As Vehicle) As String
        Return fbVehs.Find(Function(x) x.Model = veh.Model).ControlDummy2
    End Function

    <Extension>
    Public Function IsControlOutside(veh As Vehicle) As Boolean
        Return fbVehs.Find(Function(x) x.Model = veh.Model).ControlIsOutside
    End Function

    <Extension>
    Public Function IsAnyPedBlockingVehicle(veh As Vehicle) As Boolean
        Dim pos As Vector3 = veh.GetRopeHookRear
        If veh.IsVehicleFacingFlatbed(LF) Then pos = veh.GetRopeHook
        Return Native.Function.Call(Of Boolean)(Hash.IS_ANY_PED_NEAR_POINT, pos.X, pos.Y, pos.Z, 2.0F)
    End Function

    Public Sub LoadVehicles(files As String())
        Dim procFile As String = Nothing
        fbVehs.Clear()

        Try
            For Each file As String In files
                procFile = file
                Dim fd As FlatbedData = New FlatbedData(file).Instance
                Dim fv As New FlatbedVeh(fd.Model, fd.AttachDummy, fd.WinchDummy, fd.ControlDummy, fd.ControlDummy2, fd.ControlIsOutside, fd.EnableExtraDoor, fd.ExtraDoorMove, fd.ExtraDoorAngleAdjustment) ', fd.ControlDoorDummy, fd.ControlDoorDummy2)
                If Not fbVehs.Contains(fv) Then fbVehs.Add(fv)
            Next
        Catch ex As Exception
            Logger.Log($"{ex.Message} {procFile}{ex.StackTrace}")
        End Try
    End Sub

    <Extension>
    Public Function IsDriveable2(veh As Vehicle) As Boolean
        Dim result As Boolean = False
        If veh.IsDriveable Then result = True
        If veh.LockStatus = VehicleLockStatus.Unlocked Then result = True
        If veh.LockStatus = VehicleLockStatus.Locked Then result = False
        If veh.LockStatus = VehicleLockStatus.LockedForPlayer Then result = False
        Return result
    End Function

    <Extension>
    Public Sub ActivePhysics(veh As Vehicle)
        Native.Function.Call(Hash.ACTIVATE_PHYSICS, veh)
    End Sub

End Module

Public Module ReadMemory

    Public Delegate Function GetModelInfoDelegate(modelHash As Integer, indexPtr As IntPtr) As IntPtr

    Dim address As IntPtr = FindPattern("0F B7 05 ?? ?? ?? ?? 45 33 C9 4C 8B DA 66 85 C0 0F 84 ?? ?? ?? ?? 44 0F B7 C0 33 D2 8B C1 41 F7 F0 48 8B 05 ?? ?? ?? ?? 4C 8B 14 D0 EB 09 41 3B 0A 74 54")

    Public ReadOnly Property GetModelInfo() As GetModelInfoDelegate
        Get
            Return Marshal.GetDelegateForFunctionPointer(Of GetModelInfoDelegate)(address)
        End Get
    End Property

    <Extension()>
    Public Function GetVehicleMakeName(modelHash As Integer) As String
        Dim result As String = "ERROR"
        Try
            Dim index As Integer = &HFFFF
            Dim handle As GCHandle = GCHandle.Alloc(index, GCHandleType.Pinned)
            Dim modelInfo As IntPtr = GetModelInfo(modelHash, handle.AddrOfPinnedObject())
            Dim str As String = Marshal.PtrToStringAnsi(modelInfo + &H2A4)
            handle.Free()
            result = str
        Catch
            Return result
        End Try
        Return result
    End Function

    Private Function Compare(data As IntPtr, bytesPattern As Byte()) As Boolean
        For i As Integer = 0 To bytesPattern.Length - 1
            If bytesPattern(i) <> &H0 AndAlso Marshal.ReadByte(data + i) <> bytesPattern(i) Then
                Return False
            End If
        Next

        Return True
    End Function

    Public Function FindPattern(pattern As String) As IntPtr

        Dim [module] As ProcessModule = Process.GetCurrentProcess().MainModule

        Dim address As Long = [module].BaseAddress.ToInt64()
        Dim endAddress As Long = address + [module].ModuleMemorySize

        pattern = pattern.Replace(" ", "").Replace("??", "00")
        Dim bytesArray As Byte() = New Byte(pattern.Length / 2 - 1) {}
        For i As Integer = 0 To pattern.Length - 1 Step 2
            bytesArray(i / 2) = [Byte].Parse(pattern.Substring(i, 2), System.Globalization.NumberStyles.HexNumber)
        Next

        While address < endAddress
            If Compare(New IntPtr(address), bytesArray) Then
                Return New IntPtr(address)
            End If
            address += 1
        End While

        Return IntPtr.Zero
    End Function
End Module