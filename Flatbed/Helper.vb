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
    Public config As ScriptSettings = ScriptSettings.Load("scripts\Flatbed.ini")
    Public fbModel As String = "flatbed3"
    Public propModel As String = "inm_flatbed_base"
    Public marker As Boolean = True
    Public hookKey As Control = Control.VehicleSubAscend

    'Decor
    Public modDecor As String = "inm_flatbed_installed"
    Public towVehDecor As String = "inm_flatbed_vehicle"
    Public propDecor As String = "inm_flatbed_prop"
    Public bedRotXDecor As String = "inm_flatbed_rotX"
    Public bedPosYDecor As String = "inm_flatbed_posY"
    Public bedPosZDecor As String = "inm_flatbed_posZ"
    Public lastFbVehDecor As String = "inm_flatbed_last"
    Public helpDecor As String = "inm_flatbed_help"
    Public persistencePauseDecor As String = "inm_persistence_pause"

    Public slotP As Vector3 = Vector3.Zero, slotR As Vector3 = Vector3.Zero
    Public gHeight As Single = 0F

    <Extension>
    Public Sub SaveVehicleCoords(veh As Vehicle, gh As Single)
        config = ScriptSettings.Load("scripts\Flatbed.ini")
        If Not veh.IsVehicleCoordsSaved Then
            config.SetValue(Of Single)("VEHCOORDS", veh.Model.Hash.ToString, gh)
            config.Save()
        End If
    End Sub

    <Extension>
    Public Function IsVehicleCoordsSaved(veh As Vehicle) As Boolean
        Return If(config.GetValue(Of String)("VEHCOORDS", veh.Model.Hash.ToString, Nothing) = Nothing, False, True)
    End Function

    <Extension>
    Public Function GetVehicleCoords(veh As Vehicle) As Single
        config = ScriptSettings.Load("scripts\Flatbed.ini")
        Return config.GetValue(Of Single)("VEHCOORDS", veh.Model.Hash.ToString, 0F)
    End Function

    <Extension>
    Public Function IsAnyPedNearBed(prop As Prop, radius As Single) As Boolean
        Dim pos As Vector3 = prop.Position
        Return Native.Function.Call(Of Boolean)(Hash.IS_ANY_PED_NEAR_POINT, pos.X, pos.Y, pos.Z, radius)
    End Function

    <Extension>
    Public Function IsAnyVehicleNearBed(prop As Prop, radius As Single) As Boolean
        Dim pos As Vector3 = prop.Position
        Return Native.Function.Call(Of Boolean)(Hash.IS_ANY_VEHICLE_NEAR_POINT, pos.X, pos.Y, pos.Z, radius)
    End Function

    <Extension>
    Public Function IsAnyVehicleNearAttachPosition(pos As Vector3, radius As Single) As Boolean
        Return Native.Function.Call(Of Boolean)(Hash.IS_ANY_VEHICLE_NEAR_POINT, pos.X, pos.Y, pos.Z, radius)
    End Function

    <Extension>
    Public Function IsThisFlatbed3(veh As Vehicle) As Boolean
        Return veh.Model = fbModel
    End Function

    Public Sub UpdateSlotZCoords(PlusMinus As ePlusMinus, Value As Single)
        If PlusMinus = ePlusMinus.Plus Then
            slotP = New Vector3(0.0, 0.8, 0.07 + Value)
        Else
            slotP = New Vector3(0.0, 0.8, 0.07 - Value)
        End If
    End Sub

    Public Enum ePlusMinus
        Plus
        Minus
    End Enum

    <Extension>
    Public Function CurrentTowingVehicle(veh As Vehicle) As Vehicle
        Return New Vehicle(veh.GetInt(towVehDecor))
    End Function

    <Extension>
    Public Function CurrentBedProp(veh As Vehicle) As Prop
        Return New Prop(veh.GetInt(propDecor))
        'Return Native.Function.Call(Of Prop)(Hash.GET_ENTITY_ATTACHED_TO, veh)
    End Function

    <Extension>
    Public Sub CurrentTowingVehicle(flatbed As Vehicle, veh As Vehicle)
        If veh = Nothing Then flatbed.SetInt(towVehDecor, 0) Else flatbed.SetInt(towVehDecor, veh.Handle)
    End Sub

    <Extension>
    Public Sub CurrentBedProp(flatbed As Vehicle, prop As Prop)
        flatbed.SetInt(propDecor, prop.Handle)
    End Sub

    <Extension>
    Public Function GroundHeight(ent As Entity) As Single
        Return ent.HeightAboveGround
    End Function

    <Extension>
    Public Sub AttachBed(veh As Vehicle)
        Try
            Script.Wait(5000)
            veh.SetFloat(bedRotXDecor, 0F)
            veh.SetFloat(bedPosYDecor, 0F)
            veh.SetFloat(bedPosZDecor, 0F)
            If Not veh.CurrentBedProp.Handle = 0 Then veh.CurrentBedProp.Delete()
            Dim prop As Prop = World.CreateProp(propModel, veh.GetBoneCoord("misc_a"), True, False)
            prop.AttachToFix(veh, veh.GetBoneIndex("misc_a"), Vector3.Zero, Vector3.Zero)
            veh.CurrentBedProp(prop)
            veh.ToggleExtra(1, False)
        Catch ex As Exception
            Logger.Log($"{ex.Message}{ex.HResult}{ex.StackTrace}")
        End Try
    End Sub

    <Extension>
    Public Function IsFlatbedDropped(veh As Vehicle) As Boolean
        Dim result As Boolean = False
        Select Case World.GetDistance(veh.Position, veh.CurrentBedProp.Position)
            Case 3.0F To 4.0F
                result = False
            Case 7.0F To 9.0F
                result = True
        End Select
        Return result
    End Function

    <Extension>
    Public Function AttachPosition(veh As Vehicle) As Vector3
        Return veh.CurrentBedProp.Position - (veh.CurrentBedProp.ForwardVector * 6)
    End Function

    <Extension>
    Public Sub DrawMarkerTick(veh As Vehicle)
        If veh.IsFlatbedDropped AndAlso veh.CurrentTowingVehicle.Handle = 0 AndAlso Game.Player.Character.LastVehicle = veh Then
            World.DrawMarker(MarkerType.VerticalCylinder, veh.AttachPosition, Vector3.Zero, Vector3.Zero, New Vector3(2.0F, 2.0F, 3.0F), Color.GreenYellow)
        End If
    End Sub

    <Extension>
    Public Sub DropBed(veh As Vehicle)
        If veh.IsAlive Then
            Dim prop As Prop = veh.CurrentBedProp

            If Not veh.EngineRunning Then veh.EngineRunning = True
            veh.LeftIndicatorLightOn = True
            veh.RightIndicatorLightOn = True
            Dim soundId As Integer = Audio.PlaySoundFromEntity(veh, "Garage_Open", "CAR_STEAL_2_SOUNDSET")
            Script.Wait(500)

            Dim bedRotX As Single = veh.GetFloat(bedRotXDecor)
            Dim bedPosY As Single = veh.GetFloat(bedPosYDecor)
            Dim bedPosZ As Single = veh.GetFloat(bedPosZDecor)

            Select Case World.GetDistance(veh.Position, prop.Position)
                Case 3.0F To 4.0F
                    If Not prop.Handle = 0 Then
                        Do Until bedPosY <= -4.5F
                            bedPosY -= 0.03F
                            prop.AttachToFix(veh, veh.GetBoneIndex("misc_a"), New Vector3(0F, bedPosY, 0F), Vector3.Zero)
                            Script.Wait(20)
                        Loop
                    End If
                    bedPosY = -4.5F

                    If Not prop.Handle = 0 Then
                        Do Until prop.GroundHeight <= 0.5F
                            bedRotX += 0.1F
                            bedPosZ -= 0.03F
                            prop.AttachToFix(veh, veh.GetBoneIndex("misc_a"), New Vector3(0F, bedPosY, bedPosZ), New Vector3(bedRotX * 4.3F, 0F, 0F))
                            Script.Wait(20)
                        Loop
                    End If

                    veh.SetFloat(bedRotXDecor, bedRotX)
                    veh.SetFloat(bedPosYDecor, bedPosY)
                    veh.SetFloat(bedPosZDecor, bedPosZ)
                Case 7.0F To 9.0F
                    If bedPosY = 0F Then
                        bedPosY = -4.5F
                        bedRotX = 3.0F
                        bedPosZ = -0.9299996F
                    End If

                    If Not prop.Handle = 0 Then
                        Do Until bedRotX <= 0.1F
                            bedRotX -= 0.1F
                            bedPosZ += 0.03F
                            prop.AttachToFix(veh, veh.GetBoneIndex("misc_a"), New Vector3(0F, bedPosY, bedPosZ), New Vector3(bedRotX * 4.3F, 0F, 0F))
                            Script.Wait(20)
                        Loop
                    End If
                    bedRotX = 0F
                    bedPosZ = 0F

                    If Not prop.Handle = 0 Then
                        Do Until bedPosY >= 0.1F
                            bedPosY += 0.03F
                            prop.AttachToFix(veh, veh.GetBoneIndex("misc_a"), New Vector3(0F, bedPosY, 0F), Vector3.Zero)
                            Script.Wait(20)
                        Loop
                    End If
                    bedPosY = 0F

                    prop.AttachToFix(veh, veh.GetBoneIndex("misc_a"), Vector3.Zero, Vector3.Zero)

                    veh.SetFloat(bedRotXDecor, bedRotX)
                    veh.SetFloat(bedPosYDecor, bedPosY)
                    veh.SetFloat(bedPosZDecor, bedPosZ)
            End Select
            Audio.StopSound(soundId)
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
        Native.Function.Call(Hash.ATTACH_ENTITY_TO_ENTITY, entity1.Handle, entity2.Handle, boneindex, position.X, position.Y, position.Z, rotation.X, rotation.Y, rotation.Z, False, True, True, False, 2, True)
    End Sub

    <Extension>
    Public Sub AttachToPhysically(entity1 As Entity, entity2 As Entity, boneindex1 As Integer, boneindex2 As Integer, position1 As Vector3, rotation As Vector3)
        Native.Function.Call(Hash.ATTACH_ENTITY_TO_ENTITY_PHYSICALLY, entity1.Handle, entity2.Handle,
             boneindex1, boneindex2, position1.X, position1.Y, position1.Z, 0.0, 0.0, 0.0,
             rotation.X, rotation.Y, rotation.Z, 5000.0, True, False, True, True, 2)
        '                                       bForce   fRot   unk   col notelpot alwys 2
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
    Public Function GetDistanceBetweenFlatbedAndVehicleFront(Flatbed As Vehicle, Target As Vehicle) As Single
        Return World.GetDistance(Flatbed.Position, Target.Position)
    End Function

    <Extension>
    Public Function LastFlatbed(ped As Ped) As Vehicle
        Return New Vehicle(ped.GetInt(lastFbVehDecor))
    End Function

    <Extension>
    Public Sub LastFlatbed(ped As Ped, veh As Vehicle)
        ped.SetInt(lastFbVehDecor, veh.Handle)
        If veh.IsExtraOn(1) AndAlso veh.CurrentBedProp.Handle = 0 Then
            veh.AttachBed()
        End If
    End Sub

    <Extension>
    Public Sub PushDeadVehicle(veh As Vehicle)
        If veh.IsDead Then
            Native.Function.Call(Hash.APPLY_FORCE_TO_ENTITY, veh, 3, 0.0, -9.0, 0.0, 0.0, 0.0, 0.0, 0, True, True, True, True, True)
        End If
    End Sub

    Public Sub LoadSettings()
        CreateConfig()
        marker = config.GetValue(Of Boolean)("SETTING", "MARKER", True)
        fbModel = config.GetValue(Of String)("SETTING", "MODEL", "flatbed3")
        propModel = config.GetValue(Of String)("SETTING", "PROP_MODEL", "inm_flatbed_base")
        hookKey = config.GetValue(Of Control)("CONTROL", "HOOKKEY", Control.VehicleDuck)
    End Sub

    Private Sub CreateConfig()
        If Not File.Exists("scripts\Flatbed.ini") Then
            config.SetValue(Of Boolean)("SETTING", "MARKER", True)
            config.SetValue(Of String)("SETTING", "MODEL", "flatbed3")
            config.SetValue(Of String)("SETTING", "PROP_MODEL", "inm_flatbed_base")
            config.SetValue(Of Control)("CONTROL", "HOOKKEY", Control.VehicleDuck)
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