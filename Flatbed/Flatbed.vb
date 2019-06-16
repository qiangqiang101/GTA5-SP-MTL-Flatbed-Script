Imports GTA
Imports Metadata

Public Class Flatbed
    Inherits Script

    Public PP As Ped
    Public LV As Vehicle, LF As Vehicle
    Public NV As Vehicle

    Public Sub New()
        PP = Game.Player.Character
        LV = Game.Player.Character.LastVehicle

        LoadSettings()

        Decor.Unlock()
        Decor.Register(modDecor, Decor.eDecorType.Bool)
        Decor.Register(towVehDecor, Decor.eDecorType.Int)
        Decor.Register(propDecor, Decor.eDecorType.Int)
        Decor.Register(bedRotXDecor, Decor.eDecorType.Float)
        Decor.Register(bedPosYDecor, Decor.eDecorType.Float)
        Decor.Register(bedPosZDecor, Decor.eDecorType.Float)
        Decor.Register(lastFbVehDecor, Decor.eDecorType.Int)
        Decor.Register(helpDecor, Decor.eDecorType.Bool)
        Decor.Lock()
    End Sub

    Private Sub Flatbed_Tick(sender As Object, e As EventArgs) Handles Me.Tick
        Try
            RegisterDecor(modDecor, Decor.eDecorType.Bool)
            RegisterDecor(towVehDecor, Decor.eDecorType.Int)
            RegisterDecor(propDecor, Decor.eDecorType.Int)
            RegisterDecor(bedRotXDecor, Decor.eDecorType.Float)
            RegisterDecor(bedPosYDecor, Decor.eDecorType.Float)
            RegisterDecor(bedPosZDecor, Decor.eDecorType.Float)
            RegisterDecor(lastFbVehDecor, Decor.eDecorType.Int)
            RegisterDecor(helpDecor, Decor.eDecorType.Bool)

            PP = Game.Player.Character
            LV = Game.Player.Character.LastVehicle
            NV = World.GetClosestVehicle(LV.Position - (LV.ForwardVector * 2), 5.0F)
            LF = Game.Player.Character.LastFlatbed

            If Not Game.IsLoading Then
                If PP.IsInVehicle Then
                    gHeight = PP.CurrentVehicle.GroundHeight
                    PP.CurrentVehicle.SaveVehicleCoords(PP.CurrentVehicle.HeightAboveGround)

                    If PP.CurrentVehicle.IsThisFlatbed3 Then
                        PP.LastFlatbed(PP.CurrentVehicle)
                    End If
                End If

                If LV.IsThisFlatbed3 Then PP.LastFlatbed(LV)

                If Not LF.Handle = 0 Then
                    If Not LF.GetBool(helpDecor) AndAlso LV.Model = fbModel Then
                        DisplayHelpTextThisFrame(String.Format(Game.GetGXTEntry("INM_FB_HELP"), hookKey.GetButtonIcon))
                        LF.SetBool(helpDecor, True)
                    End If
                    If marker Then LF.DrawMarkerTick
                    If Cheating("ATFB") Then LF.AttachBed
                    If LF.GetFloat(bedPosYDecor) <> 0F Then LF.TurnOnIndicators Else LF.TurnOffIndicators
                    If Not LF.CurrentTowingVehicle.Handle = 0 Then
                        If Not LF.CurrentTowingVehicle.IsAttachedTo(LF.CurrentBedProp) Then LF.CurrentTowingVehicle(Nothing)
                    End If

                    If PP.IsInVehicle Then
                        If PP.CurrentVehicle = LF.CurrentTowingVehicle Then
                            LF.CurrentTowingVehicle.Detach()
                            LF.CurrentTowingVehicle.IsPersistent = False
                            LF.CurrentTowingVehicle(Nothing)
                        End If

                        If LV.IsAlive AndAlso LF.CurrentBedProp.IsAnyVehicleNearBed(2.0F) Then
                            If Not LV.IsThisFlatbed3 AndAlso LF.CurrentTowingVehicle.Handle = 0 Then
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
                                    Wait(2500)
                                    LF.CurrentTowingVehicle.AttachToPhysically(LF.CurrentBedProp, 0, 0, slotP, slotR)
                                End If
                            End If
                        End If

                        If LF.AttachPosition.IsAnyVehicleNearAttachPosition(2.0F) Then
                            If LV.IsThisFlatbed3 AndAlso LF.CurrentTowingVehicle.Handle = 0 AndAlso LF.IsFlatbedDropped Then
                                Dim thatVehicle As Vehicle = World.GetClosestVehicle(LF.AttachPosition, 2.0F)
                                If thatVehicle.Model <> LF.Model Then
                                    DisplayHelpTextThisFrame(String.Format(Game.GetGXTEntry("INM_FB_HOOK"), hookKey.GetButtonIcon, thatVehicle.FullName))
                                    If Game.IsControlJustPressed(0, hookKey) Then
                                        LF.CurrentTowingVehicle(thatVehicle)
                                        thatVehicle.EngineRunning = False
                                        thatVehicle.IsPersistent = True
                                        thatVehicle.SaveVehicleCoords(thatVehicle.GroundHeight)

                                        Game.FadeScreenOut(500)
                                        Wait(500)
                                        If Not thatVehicle.IsVehicleCoordsSaved Then
                                            thatVehicle.SaveVehicleCoords(0F)
                                        End If
                                        Dim coords As String = thatVehicle.GetVehicleCoords.ToString
                                        If coords.Contains("-") Then
                                            UpdateSlotZCoords(ePlusMinus.Minus, CSng(coords.Remove(0, 1)))
                                        Else
                                            UpdateSlotZCoords(ePlusMinus.Plus, CSng(coords))
                                        End If
                                        thatVehicle.AttachToFix(LF.CurrentBedProp, 0, slotP, slotR)
                                        thatVehicle.Detach()
                                        thatVehicle.AttachToPhysically(LF.CurrentBedProp, 0, 0, slotP, slotR)
                                        Wait(500)
                                        Game.FadeScreenIn(500)
                                    End If
                                End If
                            End If
                        End If
                    End If

                    If Not PP.IsInVehicle AndAlso LF.CurrentBedProp.IsAnyPedNearBed(3.0F) Then
                        If World.GetDistance(LF.CurrentTowingVehicle.Position, PP.Position) <= 3.0F Then
                            DisplayHelpTextThisFrame(String.Format(Game.GetGXTEntry("INM_FB_UNHOOK"), hookKey.GetButtonIcon, LF.CurrentTowingVehicle.FullName))
                            If Game.IsControlJustPressed(0, hookKey) Then
                                Select Case World.GetDistance(LF.Position, LF.CurrentBedProp.Position)
                                    Case 3.0F To 4.0F
                                        LF.DropBed
                                        LF.CurrentTowingVehicle.Detach()
                                        Wait(10)
                                        PP.Task.EnterVehicle(LF.CurrentTowingVehicle, VehicleSeat.Driver, 5000)
                                        LF.CurrentTowingVehicle.IsPersistent = False
                                        LF.CurrentTowingVehicle.PushDeadVehicle
                                        LF.CurrentTowingVehicle(Nothing)
                                    Case 7.0F To 9.0F
                                        LF.CurrentTowingVehicle.Detach()
                                        Wait(10)
                                        PP.Task.EnterVehicle(LF.CurrentTowingVehicle, VehicleSeat.Driver, 5000)
                                        LF.CurrentTowingVehicle.IsPersistent = False
                                        LF.CurrentTowingVehicle.PushDeadVehicle
                                        LF.CurrentTowingVehicle(Nothing)
                                End Select
                            End If
                        End If
                    End If

                        If PP.IsInVehicle(LF) Then
                        If Game.IsControlJustPressed(0, hookKey) Then
                            LF.DropBed
                        End If
                    End If

                    If World.GetDistance(LF.CurrentTowingVehicle.Position, LF.Position) >= 20.0F Then
                        LF.CurrentTowingVehicle.IsPersistent = False
                        LF.CurrentTowingVehicle(Nothing)
                    End If
                Else
                    If Not LF.CurrentBedProp.Handle = 0 Then LF.CurrentBedProp.Delete()
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
