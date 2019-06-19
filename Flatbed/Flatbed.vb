Imports System.Drawing
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

        Decor.Unlock()
        Decor.Register(modDecor, Decor.eDecorType.Bool)
        Decor.Register(towVehDecor, Decor.eDecorType.Int)
        Decor.Register(lastFbVehDecor, Decor.eDecorType.Int)
        Decor.Register(helpDecor, Decor.eDecorType.Bool)
        Decor.Lock()
    End Sub

    Private Sub Flatbed_Tick(sender As Object, e As EventArgs) Handles Me.Tick
        Try
            RegisterDecor(modDecor, Decor.eDecorType.Bool)
            RegisterDecor(towVehDecor, Decor.eDecorType.Int)
            RegisterDecor(lastFbVehDecor, Decor.eDecorType.Int)
            RegisterDecor(helpDecor, Decor.eDecorType.Bool)

            PP = Game.Player.Character
            LV = Game.Player.Character.LastVehicle
            NV = World.GetClosestVehicle(LV.Position - (LV.ForwardVector * 2), 5.0F)
            LF = Game.Player.Character.LastFlatbed

            If Not Game.IsLoading Then
                If PP.IsInVehicle(LF) Then
                    Game.DisableControlThisFrame(0, Control.VehicleMoveUpDown)
                    If Game.IsControlJustPressed(0, hookKey) AndAlso Not LF.AttachPosition.IsAnyVehicleNearAttachPosition(2.0F) Then LF.DropBed
                    LF.FreezePosition = False
                    LF.IsPersistent = False
                    If LF.CurrentTowingVehicle.Handle <> 0 Then
                        If Game.IsControlPressed(2, GTA.Control.VehicleAccelerate) Then 'AndAlso (LF.Speed < 10.0F OrElse System.Math.Abs(Native.Function.Call(Of Vector3)(Hash.GET_ENTITY_SPEED_VECTOR, LF, True).X) > 2.0F) Then
                            Native.Function.Call(Hash.APPLY_FORCE_TO_ENTITY, Game.Player.LastVehicle, 3, 0F, -0.04F, 0F, 0F, 0F, 0F, 0, True, True, True, True, True)
                        End If
                    End If
                Else
                    'LF.FreezePosition = True
                    LF.IsPersistent = True
                End If

                If PP.IsInVehicle Then
                    gHeight = PP.CurrentVehicle.GroundHeight
                    PP.CurrentVehicle.SaveVehicleCoords(PP.CurrentVehicle.HeightAboveGround)

                    If PP.CurrentVehicle.IsThisFlatbed3 Then PP.LastFlatbed(PP.CurrentVehicle)
                End If

                If Not LF.Handle = 0 Then
                    If Not LF.GetBool(helpDecor) AndAlso LV.Model = fbModel Then
                        DisplayHelpTextThisFrame(String.Format(Game.GetGXTEntry("INM_FB_HELP"), hookKey.GetButtonIcon))
                        LF.SetBool(helpDecor, True)
                    End If
                    If marker Then LF.DrawMarkerTick
                    If LF.IsFlatbedDropped Then LF.TurnOnIndicators Else LF.TurnOffIndicators
                    If Not LF.CurrentTowingVehicle.Handle = 0 Then
                        If Not LF.CurrentTowingVehicle.IsAttachedTo(LF) Then LF.CurrentTowingVehicle(Nothing)
                    End If

                    If PP.IsInVehicle Then
                        If PP.CurrentVehicle = LF.CurrentTowingVehicle Then
                            LF.CurrentTowingVehicle.DetachToFix()
                            LF.CurrentTowingVehicle.IsPersistent = False
                            LF.CurrentTowingVehicle(Nothing)
                        End If

                        If LV.IsAlive AndAlso LF.IsAnyPedInVehicleNearBed(2.0F) Then
                            If Not LV.IsThisFlatbed3 AndAlso LF.CurrentTowingVehicle.Handle = 0 AndAlso AC.Contains(LV.ClassType) Then
                                DisplayHelpTextThisFrame(String.Format(Game.GetGXTEntry("INM_FB_HOOK"), hookKey.GetButtonIcon, PP.CurrentVehicle.FullName))
                                If Game.IsControlJustPressed(0, hookKey) Then
                                    LF.CurrentTowingVehicle(PP.CurrentVehicle)
                                    PP.CurrentVehicle.EngineRunning = False
                                    PP.CurrentVehicle.IsPersistent = True
                                    If PP.IsInVehicle(LF.CurrentTowingVehicle) Then PP.Task.LeaveVehicle(LF.CurrentTowingVehicle, True)
                                    If Not LF.CurrentTowingVehicle.IsVehicleCoordsSaved Then
                                        LF.CurrentTowingVehicle.SaveVehicleCoords(0F)
                                    End If
                                    Dim coords As String = LF.CurrentTowingVehicle.GetVehicleCoords.ToString
                                    If coords.Contains("-") Then
                                        UpdateSlotZCoords(ePlusMinus.Minus, CSng(coords.Remove(0, 1)))
                                    Else
                                        UpdateSlotZCoords(ePlusMinus.Plus, CSng(coords))
                                    End If
                                    Wait(3000)
                                    LV.AttachToFix(LF, LF.GetBoneIndex("misc_a"), slotP, Vector3.Zero)
                                End If
                            End If
                        End If

                        If LF.AttachPosition.IsAnyVehicleNearAttachPosition(2.0F) Then
                            If Not LV.IsThisFlatbed3 AndAlso LF.CurrentTowingVehicle.Handle = 0 AndAlso LF.IsFlatbedDropped AndAlso AC.Contains(LV.ClassType) Then
                                If LV.Model <> LF.Model Then
                                    DisplayHelpTextThisFrame(String.Format(Game.GetGXTEntry("INM_FB_HOOK"), hookKey.GetButtonIcon, LV.FullName))
                                    If Game.IsControlJustPressed(0, hookKey) Then
                                        LF.CurrentTowingVehicle(LV)
                                        LV.IsPersistent = True
                                        LV.SaveVehicleCoords(LV.GroundHeight)
                                        LF.FreezePosition = True
                                        If PP.IsInVehicle(LV) Then PP.Task.LeaveVehicle()
                                        Dim rope As Rope = World.AddRope(6, LF.GetBoneCoord("misc_b"), Vector3.Zero, LF.GetBoneCoord("misc_b").DistanceTo(LV.GetRopeHook), 0.1F, False)
                                        rope.AttachEntities(LF, LF.GetBoneCoord("misc_b"), LV, LV.GetRopeHook, LF.GetBoneCoord("misc_b").DistanceTo(LV.GetRopeHook))
                                        rope.ActivatePhysics()
                                        Do Until rope.Length <= 1.9F
                                            rope.StartWinding
                                            LV.PushVehicleForward
                                            Script.Wait(5)
                                        Loop
                                        rope.StopWinding
                                        rope.DetachEntity(LF)
                                        rope.DetachEntity(LV)
                                        rope.Delete()
                                        LF.FreezePosition = False
                                        If Not LV.IsVehicleCoordsSaved Then
                                            LV.SaveVehicleCoords(0F)
                                        End If
                                        Dim coords As String = LV.GetVehicleCoords.ToString
                                        If coords.Contains("-") Then
                                            UpdateSlotZCoords(ePlusMinus.Minus, CSng(coords.Remove(0, 1)))
                                        Else
                                            UpdateSlotZCoords(ePlusMinus.Plus, CSng(coords))
                                        End If
                                        LV.AttachToFix(LF, LF.GetBoneIndex("misc_a"), slotP, Vector3.Zero)
                                    End If
                                End If
                            End If

                            If LV.IsThisFlatbed3 AndAlso LF.CurrentTowingVehicle.Handle = 0 AndAlso LF.IsFlatbedDropped Then
                                Dim thatVehicle As Vehicle = World.GetClosestVehicle(LF.AttachPosition, 2.0F)
                                If thatVehicle.Model <> LF.Model AndAlso AC.Contains(thatVehicle.ClassType) Then
                                    DisplayHelpTextThisFrame(String.Format(Game.GetGXTEntry("INM_FB_HOOK"), hookKey.GetButtonIcon, thatVehicle.FullName))
                                    If Game.IsControlJustPressed(0, hookKey) Then
                                        LF.CurrentTowingVehicle(thatVehicle)
                                        thatVehicle.IsPersistent = True
                                        thatVehicle.SaveVehicleCoords(thatVehicle.GroundHeight)
                                        LF.FreezePosition = True
                                        Dim rope As Rope = World.AddRope(6, LF.GetBoneCoord("misc_b"), Vector3.Zero, LF.GetBoneCoord("misc_b").DistanceTo(thatVehicle.GetRopeHook), 0.1F, False)
                                        rope.AttachEntities(LF, LF.GetBoneCoord("misc_b"), thatVehicle, thatVehicle.GetRopeHook, LF.GetBoneCoord("misc_b").DistanceTo(thatVehicle.GetRopeHook))
                                        rope.ActivatePhysics()
                                        Do Until rope.Length <= 1.9F
                                            rope.StartWinding
                                            thatVehicle.PushVehicleForward
                                            Script.Wait(5)
                                        Loop
                                        rope.StopWinding
                                        rope.DetachEntity(LF)
                                        rope.DetachEntity(thatVehicle)
                                        rope.Delete()
                                        LF.FreezePosition = False
                                        If Not thatVehicle.IsVehicleCoordsSaved Then
                                            thatVehicle.SaveVehicleCoords(0F)
                                        End If
                                        Dim coords As String = thatVehicle.GetVehicleCoords.ToString
                                        If coords.Contains("-") Then
                                            UpdateSlotZCoords(ePlusMinus.Minus, CSng(coords.Remove(0, 1)))
                                        Else
                                            UpdateSlotZCoords(ePlusMinus.Plus, CSng(coords))
                                        End If
                                        thatVehicle.AttachToFix(LF, LF.GetBoneIndex("misc_a"), slotP, Vector3.Zero)
                                    End If
                                End If
                            End If
                        End If
                    End If

                    If Not PP.IsInVehicle AndAlso LF.IsAnyPedNearBed(3.0F) Then
                        If World.GetDistance(LF.CurrentTowingVehicle.Position, PP.Position) <= 3.0F Then
                            DisplayHelpTextThisFrame(String.Format(Game.GetGXTEntry("INM_FB_UNHOOK"), hookKey.GetButtonIcon, LF.CurrentTowingVehicle.FullName))
                            If Game.IsControlJustPressed(0, hookKey) Then
                                LF.CurrentTowingVehicle.DetachToFix()
                                LF.FreezePosition = True
                                Do Until LF.CurrentTowingVehicle.Position.DistanceTo(LF.AttachPosition) <= 2.0F
                                    LF.CurrentTowingVehicle.PushVehicleBack
                                    Script.Wait(5)
                                Loop
                                LF.FreezePosition = False
                                LF.CurrentTowingVehicle.IsPersistent = False
                                LF.CurrentTowingVehicle(Nothing)
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