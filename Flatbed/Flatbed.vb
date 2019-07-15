Imports System.Drawing
Imports System.IO
Imports System.Net
Imports GTA
Imports GTA.Math
Imports GTA.Native
Imports Metadata

Public Class Flatbed
    Inherits Script

    Public Sub New()
        PP = Game.Player.Character
        LV = Game.Player.Character.LastVehicle

        LoadSettings()
        LoadVehicles(Directory.GetFiles(xmlPath, "*.xml"))

        Decor.Unlock()
        Decor.Register(modDecor, Decor.eDecorType.Bool)
        Decor.Register(towVehDecor, Decor.eDecorType.Int)
        Decor.Register(lastFbVehDecor, Decor.eDecorType.Int)
        Decor.Register(helpDecor, Decor.eDecorType.Bool)
        Decor.Register(gHeightDecor, Decor.eDecorType.Float)
        Decor.Register(scoopDecor, Decor.eDecorType.Float)
        Decor.Lock()
    End Sub

    Private Sub Flatbed_Tick(sender As Object, e As EventArgs) Handles Me.Tick
        Try
            RegisterDecor(modDecor, Decor.eDecorType.Bool)
            RegisterDecor(towVehDecor, Decor.eDecorType.Int)
            RegisterDecor(lastFbVehDecor, Decor.eDecorType.Int)
            RegisterDecor(helpDecor, Decor.eDecorType.Bool)
            RegisterDecor(gHeightDecor, Decor.eDecorType.Float)
            RegisterDecor(scoopDecor, Decor.eDecorType.Float)

            PP = Game.Player.Character
            LV = Game.Player.Character.LastVehicle
            LF = Game.Player.Character.LastFlatbed
            NV = World.GetClosestVehicle(LF.Position - (LF.ForwardVector * 2), 5.0F)

            If Not Game.IsLoading Then
                If PP.IsInVehicle(LF) Then
                    Game.DisableControlThisFrame(0, Control.VehicleMoveUpDown)
                    If Not LF.IsControlOutside Then
                        If manualControl Then
                            If Game.IsControlPressed(0, liftKey) Then LF.DropBedManually(True)
                            If Game.IsControlPressed(0, lowerKey) Then LF.DropBedManually(False)
                        Else
                            If Game.IsControlJustPressed(0, hookKey) Then LF.DropBed
                        End If
                    End If
                    LF.FreezePosition = False
                    LF.IsPersistent = False
                    If LF.CurrentTowingVehicle.Handle <> 0 Then
                        If Game.IsControlPressed(2, Control.VehicleAccelerate) Then
                            Native.Function.Call(Hash.APPLY_FORCE_TO_ENTITY, Game.Player.LastVehicle, 3, 0F, -0.04F, 0F, 0F, 0F, 0F, 0, True, True, True, True, True)
                        End If
                        If Game.IsControlPressed(2, Control.VehicleBrake) Then
                            Native.Function.Call(Hash.APPLY_FORCE_TO_ENTITY, Game.Player.LastVehicle, 3, 0F, 0.04F, 0F, 0F, 0F, 0F, 0, True, True, True, True, True)
                        End If
                    End If
                Else
                    LF.IsPersistent = True
                End If

                If PP.IsInVehicle Then
                    If PP.CurrentVehicle.IsOnAllWheels AndAlso PP.CurrentVehicle.GetFloat(gHeightDecor) = 0F Then PP.CurrentVehicle.SetFloat(gHeightDecor, PP.CurrentVehicle.HeightAboveGround)
                    If PP.CurrentVehicle.IsThisFlatbed3 Then PP.LastFlatbed(PP.CurrentVehicle)
                End If

                If Not LF.Handle = 0 Then
                    If Not PP.IsInVehicle(LF) AndAlso LF.IsControlOutside AndAlso (PP.Position.DistanceTo(LF.ControlDummyPos) <= 2.0F Or PP.Position.DistanceTo(LF.ControlDummy2Pos) <= 2.0F) Then
                        If manualControl Then
                            DisplayHelpTextThisFrame(String.Format(GetLangEntry("INM_FB_HELP"), $"{liftKey.GetButtonIcon} {lowerKey.GetButtonIcon}"))
                            If Game.IsControlPressed(0, liftKey) Then LF.DropBedManually(True)
                            If Game.IsControlPressed(0, lowerKey) Then LF.DropBedManually(False)
                        Else
                            DisplayHelpTextThisFrame(String.Format(GetLangEntry("INM_FB_HELP"), hookKey.GetButtonIcon))
                            If Game.IsControlJustPressed(0, hookKey) Then LF.DropBed
                        End If
                    End If

                    If Not LF.GetBool(helpDecor) AndAlso fbVehs.Contains(fbVehs.Find(Function(x) x.Model = LV.Model)) Then
                        If manualControl Then
                            DisplayHelpTextThisFrame(String.Format(GetLangEntry("INM_FB_HELP"), $"{liftKey.GetButtonIcon} {lowerKey.GetButtonIcon}"))
                        Else
                            DisplayHelpTextThisFrame(String.Format(GetLangEntry("INM_FB_HELP"), hookKey.GetButtonIcon))
                        End If

                        LF.SetBool(helpDecor, True)
                    End If
                    If marker Then LF.DrawMarkerTick
                    If LF.IsFlatbedDropped Then LF.TurnOnIndicators Else LF.TurnOffIndicators
                    If Not LF.CurrentTowingVehicle.Handle = 0 Then
                        If Not LF.CurrentTowingVehicle.IsAttachedTo(LF) Then LF.CurrentTowingVehicle(Nothing)
                    End If

                    If PP.IsInVehicle Then
                        If PP.CurrentVehicle = LF.CurrentTowingVehicle Then
                            If LV.IsVehicleFacingFlatbed(LF) Then
                                LF.CurrentTowingVehicle.DetachToFix(False)
                            Else
                                LF.CurrentTowingVehicle.DetachToFix(True)
                            End If
                            LF.CurrentTowingVehicle.IsPersistent = False
                            LF.CurrentTowingVehicle(Nothing)
                        End If

                        If PP.IsInVehicle() AndAlso LV.Position.DistanceTo(LF.AttachDummyPos) <= 2.0F Then
                            If Not LV.IsThisFlatbed3 AndAlso LF.CurrentTowingVehicle.Handle = 0 AndAlso AC.Contains(LV.ClassType) Then
                                DisplayHelpTextThisFrame(String.Format(GetLangEntry("INM_FB_HOOK"), hookKey.GetButtonIcon, PP.CurrentVehicle.FullName))
                                If Game.IsControlJustPressed(0, hookKey) Then
                                    LF.CurrentTowingVehicle(PP.CurrentVehicle)
                                    PP.CurrentVehicle.EngineRunning = False
                                    PP.CurrentVehicle.IsPersistent = True
                                    If PP.IsInVehicle(LF.CurrentTowingVehicle) Then PP.Task.LeaveVehicle(LF.CurrentTowingVehicle, True)
                                    Wait(3000)
                                    If LV.IsVehicleFacingFlatbed(LF) Then
                                        LV.AttachToFix(LF, LF.AttachDummyIndex, LV.AttachCoords, Vector3.Zero)
                                    Else
                                        LV.AttachToFix(LF, LF.AttachDummyIndex, LV.AttachCoords, New Vector3(0F, 0F, 180.0F))
                                    End If
                                End If
                            End If
                        End If

                        If LF.AttachPosition.IsAnyVehicleNearAttachPosition(2.0F) Then
                            If Not LV.IsThisFlatbed3 AndAlso LF.CurrentTowingVehicle.Handle = 0 AndAlso LF.IsFlatbedDropped AndAlso AC.Contains(LV.ClassType) Then
                                If LV.Model <> LF.Model Then
                                    DisplayHelpTextThisFrame(String.Format(GetLangEntry("INM_FB_HOOK"), hookKey.GetButtonIcon, LV.FullName))
                                    If Game.IsControlJustPressed(0, hookKey) Then
                                        LF.CurrentTowingVehicle(LV)
                                        LV.IsPersistent = True
                                        If LV.IsOnAllWheels AndAlso LV.GetFloat(gHeightDecor) = 0F Then LV.SetFloat(gHeightDecor, LV.HeightAboveGround)
                                        LF.FreezePosition = True
                                        If PP.IsInVehicle(LV) Then PP.Task.LeaveVehicle()
                                        If LV.IsVehicleFacingFlatbed(LF) Then
                                            While Not PP.Position.DistanceTo(LV.GetRopeHook) <= 1.5F
                                                PP.Task.GoTo(LV.GetRopeHook)
                                                Wait(100)
                                            End While
                                        Else
                                            While Not PP.Position.DistanceTo(LV.GetRopeHookRear) <= 1.5F
                                                PP.Task.GoTo(LV.GetRopeHookRear)
                                                Wait(100)
                                            End While
                                        End If
                                        PP.Task.ClearAll()
                                        If LV.IsVehicleFacingFlatbed(LF) Then
                                            Dim rope As Rope = World.AddRope(6, LF.WinchDummyPos, Vector3.Zero, LF.WinchDummyPos.DistanceTo(LV.GetRopeHook), 0.1F, False)
                                            rope.AttachEntities(LF, LF.WinchDummyPos, LV, LV.GetRopeHook, LF.WinchDummyPos.DistanceTo(LV.GetRopeHook))
                                            rope.ActivatePhysics()
                                            Do Until rope.Length <= 1.9F
                                                If Not LV.IsAnyPedBlockingVehicle Then
                                                    rope.StartWinding
                                                    Game.DisableControlThisFrame(0, Control.VehicleMoveUpDown)
                                                    If Cheating("stop fb") Then
                                                        rope.Delete()
                                                        LF.FreezePosition = False
                                                        LF.CurrentTowingVehicle(Nothing)
                                                        Exit Sub
                                                    End If
                                                    Wait(5)
                                                Else
                                                    rope.StopWinding
                                                    Wait(5)
                                                End If
                                            Loop
                                            rope.StopWinding
                                            rope.DetachEntity(LF)
                                            rope.DetachEntity(LV)
                                            rope.Delete()
                                            LF.FreezePosition = False
                                            LV.AttachToFix(LF, LF.AttachDummyIndex, LV.AttachCoords, Vector3.Zero)
                                        Else
                                            Dim rope As Rope = World.AddRope(6, LF.WinchDummyPos, Vector3.Zero, LF.WinchDummyPos.DistanceTo(LV.GetRopeHookRear), 0.1F, False)
                                            rope.AttachEntities(LF, LF.WinchDummyPos, LV, LV.GetRopeHookRear, LF.WinchDummyPos.DistanceTo(LV.GetRopeHookRear))
                                            rope.ActivatePhysics()
                                            Do Until rope.Length <= 1.9F
                                                If Not LV.IsAnyPedBlockingVehicle Then
                                                    rope.StartWinding
                                                    Game.DisableControlThisFrame(0, Control.VehicleMoveUpDown)
                                                    If Cheating("stop fb") Then
                                                        rope.Delete()
                                                        LF.FreezePosition = False
                                                        LF.CurrentTowingVehicle(Nothing)
                                                        Exit Sub
                                                    End If
                                                    Wait(5)
                                                Else
                                                    rope.StopWinding
                                                    Wait(5)
                                                End If
                                            Loop
                                            rope.StopWinding
                                            rope.DetachEntity(LF)
                                            rope.DetachEntity(LV)
                                            rope.Delete()
                                            LF.FreezePosition = False
                                            LV.AttachToFix(LF, LF.AttachDummyIndex, LV.AttachCoords, New Vector3(0F, 0F, 180.0F))
                                        End If
                                    End If
                                End If
                            End If
                        End If
                    End If

                    If Not PP.IsInVehicle AndAlso LF.IsFlatbedDropped AndAlso PP.Position.DistanceTo(LF.AttachDummyPos) <= 3.0F Then
                        If World.GetDistance(LF.CurrentTowingVehicle.Position, PP.Position) <= 3.0F Then
                            DisplayHelpTextThisFrame(String.Format(GetLangEntry("INM_FB_UNHOOK"), hookKey.GetButtonIcon, LF.CurrentTowingVehicle.FullName))
                            If Game.IsControlJustPressed(0, hookKey) Then
                                Dim towVeh As Vehicle = LF.CurrentTowingVehicle
                                LF.FreezePosition = True
                                towVeh.SteeringScale = 0F
                                If towVeh.IsVehicleFacingFlatbed(LF) Then
                                    towVeh.DetachToFix(False)
                                Else
                                    towVeh.DetachToFix(True)
                                End If
                                Wait(1000)
                                If towVeh.IsDriveable2 Then
                                    PP.Task.EnterVehicle(towVeh, VehicleSeat.Driver, 5000, 1.0F)
                                    Do Until towVeh.Position.DistanceTo(LF.AttachPosition) <= 2.0F
                                        Game.DisableControlThisFrame(0, Control.VehicleMoveUpDown)
                                        Script.Wait(5)
                                    Loop
                                Else
                                    Do Until towVeh.Position.DistanceTo(LF.AttachPosition) <= 2.0F
                                        If towVeh.IsVehicleFacingFlatbed(LF) Then
                                            towVeh.PushVehicleBack
                                        Else
                                            towVeh.PushVehicleForward
                                        End If
                                        Game.DisableControlThisFrame(0, Control.VehicleMoveUpDown)
                                        Script.Wait(5)
                                    Loop
                                End If
                                LF.FreezePosition = False
                                towVeh.IsPersistent = False
                                LF.CurrentTowingVehicle(Nothing)
                            End If
                        End If
                    End If

                    If Not PP.IsInVehicle AndAlso LF.CurrentTowingVehicle.Handle = 0 AndAlso LF.IsFlatbedDropped AndAlso LF.AttachPosition.IsAnyVehicleNearAttachPosition(2.0F) Then
                        Dim thatVehicle As Vehicle = World.GetClosestVehicle(LF.AttachPosition, 2.0F)
                        If thatVehicle.Model <> LF.Model AndAlso AC.Contains(thatVehicle.ClassType) AndAlso PP.Position.DistanceTo(thatVehicle.GetRopeHook) <= 1.5F Then
                            DisplayHelpTextThisFrame(String.Format(GetLangEntry("INM_FB_HOOK"), hookKey.GetButtonIcon, thatVehicle.FullName))
                            If Game.IsControlJustPressed(0, hookKey) Then
                                LF.CurrentTowingVehicle(thatVehicle)
                                thatVehicle.IsPersistent = True
                                If thatVehicle.IsOnAllWheels AndAlso thatVehicle.GetFloat(gHeightDecor) = 0F Then thatVehicle.SetFloat(gHeightDecor, thatVehicle.HeightAboveGround)
                                LF.FreezePosition = True
                                Dim rope As Rope = World.AddRope(6, LF.WinchDummyPos, Vector3.Zero, LF.WinchDummyPos.DistanceTo(thatVehicle.GetRopeHook), 0.1F, False)
                                rope.AttachEntities(LF, LF.WinchDummyPos, thatVehicle, thatVehicle.GetRopeHook, LF.WinchDummyPos.DistanceTo(thatVehicle.GetRopeHook))
                                rope.ActivatePhysics()
                                Do Until rope.Length <= 1.9F
                                    If Not thatVehicle.IsAnyPedBlockingVehicle Then
                                        rope.StartWinding
                                        Game.DisableControlThisFrame(0, Control.VehicleMoveUpDown)
                                        If Cheating("stop fb") Then
                                            rope.Delete()
                                            LF.FreezePosition = False
                                            LF.CurrentTowingVehicle(Nothing)
                                            Exit Sub
                                        End If
                                        Wait(5)
                                    Else
                                        rope.StopWinding
                                        Wait(5)
                                    End If
                                Loop
                                rope.StopWinding
                                rope.DetachEntity(LF)
                                rope.DetachEntity(thatVehicle)
                                rope.Delete()
                                LF.FreezePosition = False
                                thatVehicle.AttachToFix(LF, LF.AttachDummyIndex, thatVehicle.AttachCoords, Vector3.Zero)
                            End If
                        End If

                        If thatVehicle.Model <> LF.Model AndAlso AC.Contains(thatVehicle.ClassType) AndAlso PP.Position.DistanceTo(thatVehicle.GetRopeHookRear) <= 1.5F Then
                            DisplayHelpTextThisFrame(String.Format(GetLangEntry("INM_FB_HOOK"), hookKey.GetButtonIcon, thatVehicle.FullName))
                            If Game.IsControlJustPressed(0, hookKey) Then
                                LF.CurrentTowingVehicle(thatVehicle)
                                thatVehicle.IsPersistent = True
                                If thatVehicle.IsOnAllWheels AndAlso thatVehicle.GetFloat(gHeightDecor) = 0F Then thatVehicle.SetFloat(gHeightDecor, thatVehicle.HeightAboveGround)
                                LF.FreezePosition = True
                                Dim rope As Rope = World.AddRope(6, LF.WinchDummyPos, Vector3.Zero, LF.WinchDummyPos.DistanceTo(thatVehicle.GetRopeHookRear), 0.1F, False)
                                rope.AttachEntities(LF, LF.WinchDummyPos, thatVehicle, thatVehicle.GetRopeHookRear, LF.WinchDummyPos.DistanceTo(thatVehicle.GetRopeHookRear))
                                rope.ActivatePhysics()
                                Do Until rope.Length <= 1.9F
                                    If Not thatVehicle.IsAnyPedBlockingVehicle Then
                                        rope.StartWinding
                                        Game.DisableControlThisFrame(0, Control.VehicleMoveUpDown)
                                        If Cheating("stop fb") Then
                                            rope.Delete()
                                            LF.FreezePosition = False
                                            LF.CurrentTowingVehicle(Nothing)
                                            Exit Sub
                                        End If
                                        Wait(5)
                                    Else
                                        rope.StopWinding
                                        Wait(5)
                                    End If
                                Loop
                                rope.StopWinding
                                rope.DetachEntity(LF)
                                rope.DetachEntity(thatVehicle)
                                rope.Delete()
                                LF.FreezePosition = False
                                thatVehicle.AttachToFix(LF, LF.AttachDummyIndex, thatVehicle.AttachCoords, New Vector3(0F, 0F, 180.0F))
                            End If
                        End If

                    End If

                    If World.GetDistance(LF.CurrentTowingVehicle.Position, LF.Position) >= 20.0F Then
                        LF.CurrentTowingVehicle.IsPersistent = False
                        LF.CurrentTowingVehicle(Nothing)
                    End If
                End If
            End If
        Catch ex As Exception
            Logger.Log($"{ex.Message}{ex.HResult}{ex.StackTrace}")
        End Try
    End Sub

    Private Sub Flatbed_Aborted(sender As Object, e As EventArgs) Handles Me.Aborted
        Game.FadeScreenIn(300)
    End Sub

End Class