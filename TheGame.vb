﻿Imports System.Drawing.Drawing2D
Imports System.IO
Imports System.Security.Cryptography.X509Certificates
Imports System.Windows.Forms.VisualStyles
Imports System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox
Imports Newtonsoft.Json
Imports System.Media

Public Class TheGame

    Public noofplayers As Integer

    Public PName As New List(Of String)

    Private nomatches As Integer

    Private angle As Integer = 0

    Dim p1 As PlayerInfo
    Dim p2 As PlayerInfo
    Dim p3 As PlayerInfo
    Dim p4 As PlayerInfo

    Private SFXplayer As SoundPlayer

    Dim draw_deck_JSON_FilePath As String = "D:\_Programs\_Visual_Studio_Workspace\Game\Resources\TempGameData\draw_deck.json"
    Dim players_hands_JSON_FilePath As String = "D:\_Programs\_Visual_Studio_Workspace\Game\Resources\TempGameData\players_hands.json"
    Dim unmatched_Cards_JSON_FilePath As String = "D:\_Programs\_Visual_Studio_Workspace\Game\Resources\TempGameData\unmatched_cards.json"

    Dim soundFilePath As String = "D:\_Programs\_Visual_Studio_Workspace\Game\Resources\Sfx\Explosion_FX.wav"
    'Dim horizontalDeck_of_Cards_FilePath As String = "Resources\Deck_of_Cards\Horizontal\"
    'Dim verticalDeck_of_Cards_FilePath As String = "Resources\Deck_of_Cards\Vertical\"

    Private Async Sub TheGame_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Timer1.Interval = 45 ' Animation speed/Adjust timer interval for smoother animation

        SFXplayer = New SoundPlayer(soundFilePath)

        MsgBox("Welcome Every-nyan!!!")
        Draw.Hide()
        Await Task.Delay(1000)
        Initialize_Game()
    End Sub

    Private Sub PlaySound()
        Try
            ' Play the sound
            SFXplayer.Play()
        Catch ex As Exception
            ' Handle exceptions, e.g., file not found
            MessageBox.Show("Error playing sound: " & ex.Message)
        End Try
    End Sub

    Private Sub TheGame_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
        Entry.Clear.Enabled = True
        Entry.Clear.Show()
        Entry.Show()
    End Sub

    Private Sub Initialize_Game()
        Try
            ' Clear existing JSON files
            ClearJsonFile(draw_deck_JSON_FilePath)
            ClearJsonFile(unmatched_Cards_JSON_FilePath)

            Reshuffle.Hide()
            CenterDeck.Show()
            DeckVisible()

            ' Initialize the deck
            Dim deck As List(Of String) = Initializedeck()

            ' Shuffle the deck
            deck = Shuffledeck(deck)

            ' Distribute cards to players (5 cards each)
            Dim cardsPerPlayer As Integer = 5
            Dim players As List(Of List(Of String)) = Distributecards(deck, noofplayers, cardsPerPlayer)

            ' Save each player's hand to a JSON file
            SavePlayerHands(players)

            ' Save remaining cards to draw_deck.json
            Dim playerHands As New List(Of String)()
            For Each player In players
                playerHands.AddRange(player)
            Next
            Dim remainingDeck As List(Of String) = deck.Except(playerHands).ToList()
            Writeremainingcardstojsonfile(remainingDeck, draw_deck_JSON_FilePath)

            ' Initialize the players
            Initialize_PlayingTable()

            ' Load player information
            LoadPlayers(noofplayers)

            ' MsgBox("Game initialization completed.")
        Catch ex As Exception
            MsgBox("An error occurred during game initialization: " & ex.Message)
        End Try
    End Sub

    Private Async Sub Draw_Click(sender As Object, e As EventArgs) Handles Draw.Click
        Try
            Draw.Hide()

            ' Draw a random card from draw_deck.json
            Dim randomCard As String = GetRandomCardFromFile(draw_deck_JSON_FilePath)

            ' Display the drawn card image and hide the draw button
            CardDrew.Show()
            DrawnCardImage(randomCard)

            ' Hold the Card image for a second
            Await Task.Delay(1000)

            ' Check for matches with the drawn card
            CheckForMatch(randomCard)

            CardDrew.Hide()

            ' Check if current player has no more cards
            Dim currentPlayer As PlayerInfo = ReadPlayerInfoFromJson(1)
            If currentPlayer.Cards.Count = 0 Then
                GameFinished(currentPlayer.Name)
                Exit Sub
            End If

            ' Delay before hiding center elements
            Await Task.Delay(500)
            CenterDeck.Hide()

            ' Rotate turn to the next player
            Turn_rotate()

            ' Delay before changing turn and reloading player info
            Await Task.Delay(900)
            ChangeTurn()
            LoadPlayers(noofplayers)

            If IsJsonEmpty(draw_deck_JSON_FilePath) Then
                Reshuffle.Show()
                Exit Sub
            End If

            Draw.Show()
            Draw.Focus()

        Catch ex As Exception
            MsgBox("An error occurred during card drawing: " & ex.Message)
        End Try
    End Sub

    Private Sub CheckForMatch(drawnCard As String)
        Dim currentPlayer As PlayerInfo = ReadPlayerInfoFromJson(1S)
        Dim currentPlayerCards As New List(Of String)()
        Dim cardsToRemove As New List(Of String)
        ' Reset nomatches count
        nomatches = 0

        If currentPlayer IsNot Nothing Then
            For Each card In currentPlayer.Cards
                currentPlayerCards.Add(card)
            Next

            For i = 0 To currentPlayerCards.Count - 2
                Dim card1 = currentPlayerCards(i)

                ' Check for duplicates starting from the next card in the list
                For j = i + 1 To currentPlayerCards.Count - 1
                    Dim card2 = currentPlayerCards(j)

                    If card1.Substring(0, 1) = card2.Substring(0, 1) Then
                        ' Found a duplicate, add both cards to remove list
                        cardsToRemove.Add(card1)
                        cardsToRemove.Add(card2)
                    End If
                Next
            Next

            ' Remove duplicate cards from player hands
            For Each cardToRemove In cardsToRemove
                RemoveCardFromFile(players_hands_JSON_FilePath, cardToRemove)
            Next

            For Each card In currentPlayerCards

                If drawnCard.Substring(0, 1) = card.Substring(0, 1) Then
                    ' PlaySound()                                                                       Enable later
                    MsgBox("Hit!!")
                    RemoveCardFromFile(draw_deck_JSON_FilePath, drawnCard)
                    RemoveCardFromFile(players_hands_JSON_FilePath, card)

                    LoadPlayers(noofplayers)
                    Exit Sub ' Exit sub once a match is found and removed
                Else
                    nomatches += 1
                End If
            Next
            ' If no matches, add the card to unmatched_cards.json
            If nomatches = currentPlayerCards.Count Then
                RemoveCardFromFile(draw_deck_JSON_FilePath, drawnCard)
                AddCardToFile(unmatched_Cards_JSON_FilePath, drawnCard)
            End If
        Else

            MsgBox("Error! Current player is empty.")

        End If

    End Sub

    Private Sub Initialize_PlayingTable()
        If noofplayers = 2 Then
            Player_2.Enabled = False
            Player_2.Hide()
            Player2.Enabled = False
            Player2.Hide()

            Player_4.Enabled = False
            Player_4.Hide()
            Player4.Enabled = False
            Player4.Hide()

        ElseIf noofplayers = 3 Then
            Player_3.Enabled = False
            Player_3.Hide()
            Player3.Enabled = False
            Player3.Hide()
        End If
    End Sub

    Private Sub LoadPlayers(noofplayers As Integer)
        If noofplayers = 2 Then
            LoadPlayer1CardsIntoPictureBoxes(1)
            Player1.Invalidate()
            LoadPlayer3CardsIntoPictureBoxes(2)
            Player3.Invalidate()
        ElseIf noofplayers = 3 Then
            LoadPlayer1CardsIntoPictureBoxes(1)
            Player1.Invalidate()
            LoadPlayer2CardsIntoPictureBoxes(2)
            Player2.Invalidate()
            LoadPlayer4CardsIntoPictureBoxes(3)
            Player4.Invalidate()
        ElseIf noofplayers = 4 Then
            LoadPlayer1CardsIntoPictureBoxes(1)
            Player1.Invalidate()
            LoadPlayer2CardsIntoPictureBoxes(2)
            Player2.Invalidate()
            LoadPlayer3CardsIntoPictureBoxes(3)
            Player3.Invalidate()
            LoadPlayer4CardsIntoPictureBoxes(4)
            Player4.Invalidate()
        Else
            MsgBox("Error in number of players")
        End If
    End Sub

    Private Sub ChangeTurn()
        Dim players As List(Of PlayerInfo) = ReadAllPlayersFromJson(players_hands_JSON_FilePath)
        For Each player In players
            player.Position -= 1
            If noofplayers = 2 Then
                If player.Position = 0 Then
                    player.Position = 2
                End If
            ElseIf noofplayers = 3 Then
                If player.Position = 0 Then
                    player.Position = 3
                End If
            Else
                If player.Position = 0 Then
                    player.Position = 4
                End If
            End If
        Next
        Dim updatedTurn As String = JsonConvert.SerializeObject(players, Formatting.Indented)
        File.WriteAllText(players_hands_JSON_FilePath, updatedTurn)
    End Sub

    Sub DrawnCardImage(cardName As String)
        Try
            Dim imagePath As String = $"D:\_Programs\_Visual_Studio_Workspace\Game\Resources\Deck_of_Cards\Vertical\{cardName}_Vrtl.png"
            CardDrew.Image = Image.FromFile(imagePath)
            CardDrew.SizeMode = PictureBoxSizeMode.StretchImage
        Catch ex As FileNotFoundException
            MsgBox("Image file Not found for " & cardName)
        End Try
    End Sub

    Sub ClearJsonFile(filePath As String)
        ' Serialize an empty array 
        Dim emptyJson As String = "[]"
        File.WriteAllText(filePath, emptyJson)
    End Sub

    Function Initializedeck() As List(Of String)
        Dim suits As String() = {"Hearts", "Diamonds", "Clubs", "Spades"}
        Dim values As String() = {"2", "3", "4", "5", "6", "7", "8", "9", "10", "Jack", "Queen", "King", "Ace"}
        Dim deck As New List(Of String)()

        For Each suit In suits
            For Each value In values
                deck.Add(value & " of " & suit)
            Next
        Next

        Return deck
    End Function

    Function Shuffledeck(deck As List(Of String)) As List(Of String)
        Dim rand As New Random()
        Dim n As Integer = deck.Count

        While n > 1
            n -= 1
            Dim k As Integer = rand.Next(n + 1)
            Dim value As String = deck(k)
            deck(k) = deck(n)
            deck(n) = value
        End While

        Return deck
    End Function

    Private Sub Reshuffle_Click(sender As Object, e As EventArgs) Handles Reshuffle.Click
        Reshuffle.Hide()
        CardDrew.Hide()
        DeckVisible()

        Dim unmatched_cards As String = File.ReadAllText(unmatched_Cards_JSON_FilePath)
        Dim draw_deck As String = File.ReadAllText(draw_deck_JSON_FilePath)

        Dim cards1 As List(Of String) = JsonConvert.DeserializeObject(Of List(Of String))(unmatched_cards)
        Dim cards2 As List(Of String) = JsonConvert.DeserializeObject(Of List(Of String))(draw_deck)

        For Each card In cards1
            cards2.Add(card)
        Next

        Dim updatedJson As String = JsonConvert.SerializeObject(Shuffledeck(cards2), Formatting.Indented)

        File.WriteAllText(draw_deck_JSON_FilePath, updatedJson)

        ClearJsonFile(unmatched_Cards_JSON_FilePath)
    End Sub

    Function Distributecards(deck As List(Of String), numberofplayers As Integer, cardsperplayer As Integer) As List(Of List(Of String))
        Dim players As New List(Of List(Of String))()
        Dim totalCards As Integer = numberofplayers * cardsperplayer

        If totalCards > deck.Count Then
            Throw New ArgumentException("Not enough cards in the deck to distribute.")
        End If

        For i As Integer = 0 To numberofplayers - 1
            Dim playercards As New List(Of String)()

            For j As Integer = 0 To cardsperplayer - 1
                playercards.Add(deck(i * cardsperplayer + j))
            Next

            players.Add(playercards)
        Next

        ' Remove the distributed cards from the deck
        deck.RemoveRange(0, totalCards)

        Return players
    End Function

    Sub Writeremainingcardstojsonfile(remainingdeck As List(Of String), filepath As String)
        Dim json As String = JsonConvert.SerializeObject(remainingdeck, Formatting.Indented)
        File.WriteAllText(filepath, json)
        'MsgBox("Remaining cards saved to " & filepath)
    End Sub

    Sub SavePlayerHands(players As List(Of List(Of String)))
        Try
            ' Convert list of lists of strings to list of Player objects
            Dim playersList As New List(Of PlayerInfo)
            For i As Integer = 0 To players.Count - 1
                playersList.Add(New PlayerInfo With {
                    .Position = (i + 1),
                    .Name = PName(i),
                    .Cards = players(i)
                })
            Next

            ' Serialize list of Player objects to JSON
            Dim json As String = JsonConvert.SerializeObject(playersList, Formatting.Indented)

            ' Write JSON to file
            File.WriteAllText(players_hands_JSON_FilePath, json)

            'MsgBox("Player hands saved successfully.")
        Catch ex As Exception
            MsgBox($"Error saving player hands:    {ex.Message}")
        End Try
    End Sub

    Function GetRandomCardFromFile(filePath As String) As String
        Try
            ' Check if the file exists
            If Not File.Exists(filePath) Then
                Throw New FileNotFoundException("The file was not found: " & filePath)
            End If

            ' Read all text from JSON file
            Dim json As String = File.ReadAllText(filePath)

            ' Check if the JSON is empty
            If String.IsNullOrWhiteSpace(json) Then
                Throw New Exception("The file is empty or contains only whitespace: " & filePath)
            End If

            ' Deserialize JSON array to List(Of String)
            Dim cards As List(Of String) = JsonConvert.DeserializeObject(Of List(Of String))(json)

            If cards Is Nothing OrElse cards.Count = 4 Then
                DeckCard4.Hide()
            ElseIf cards Is Nothing OrElse cards.Count = 3 Then
                DeckCard4.Hide()
                DeckCard3.Hide()
            ElseIf cards Is Nothing OrElse cards.Count = 2 Then
                DeckCard4.Hide()
                DeckCard3.Hide()
                DeckCard2.Hide()
            ElseIf cards Is Nothing OrElse cards.Count = 1 Then
                DeckCard4.Hide()
                DeckCard3.Hide()
                DeckCard2.Hide()
                DeckCard1.Hide()
            End If

            ' Generate random index
            Dim rand As New Random()
            Dim randomIndex As Integer = rand.Next(0, cards.Count)

            ' Return random card
            Return cards(randomIndex)
        Catch ex As Exception
            ' Log the error message (you can replace this with proper logging)
            MsgBox("Error: " & ex.Message)
            Return String.Empty
        End Try
    End Function

    Sub AddCardToFile(filePath As String, cardToAdd As String)
        ' Read all text from JSON file
        Dim json As String = File.ReadAllText(filePath)

        ' Deserialize JSON array to List(Of String)
        Dim cards As List(Of String) = JsonConvert.DeserializeObject(Of List(Of String))(json)

        ' Add the new card to the list
        cards.Add(cardToAdd)

        ' Serialize updated list back to JSON
        Dim updatedJson As String = JsonConvert.SerializeObject(cards, Formatting.Indented)

        ' Write updated JSON back to file
        File.WriteAllText(filePath, updatedJson)
    End Sub

    Public Async Sub DeckVisible()
        Draw.Hide()
        DeckCard4.Hide()
        Await Task.Delay(100)
        DeckCard4.Show()
        DeckCard3.Hide()
        Await Task.Delay(100)
        DeckCard3.Show()
        DeckCard2.Hide()
        Await Task.Delay(100)
        DeckCard2.Show()
        DeckCard1.Hide()
        Await Task.Delay(100)
        DeckCard1.Show()
        Await Task.Delay(300)
        Draw.Show()
    End Sub

    Private Sub LoadPlayer1CardsIntoPictureBoxes(P1position As Integer)
        ' Read player information from JSON
        p1 = ReadPlayerInfoFromJson(P1position)
        Dim noofcards As Integer = p1.Cards.Count

        If p1 IsNot Nothing Then

            ' Access player cards and load into picture boxes
            Dim pictureBoxIndex As Integer = 1
            For Each card In p1.Cards
                If pictureBoxIndex <= noofcards Then
                    Dim pictureBoxName As String = $"Player1Card{pictureBoxIndex}"
                    Dim pictureBox As PictureBox = Me.Controls.Find(pictureBoxName, True).FirstOrDefault()

                    If pictureBox IsNot Nothing Then
                        Dim imagePath As String = $"D:\_Programs\_Visual_Studio_Workspace\Game\Resources\Deck_of_Cards\Vertical\{card}_Vrtl.png" ' Adjust file extension as per your setup

                        If File.Exists(imagePath) Then
                            pictureBox.Image = Image.FromFile(imagePath)
                        Else
                            MsgBox($"Image file not found: {imagePath}")
                        End If
                    Else
                        MsgBox($"PictureBox not found: {pictureBoxName}")
                    End If

                    pictureBoxIndex += 1
                Else
                    Exit For
                End If
            Next

            ' Fill remaining picture boxes with default image
            Dim defaultImagePath As String = "D:\_Programs\_Visual_Studio_Workspace\Game\Resources\Vertical_Card.jpg"

            While pictureBoxIndex <= 5
                Dim pictureBoxName As String = $"Player1Card{pictureBoxIndex}"
                Dim pictureBox As PictureBox = Me.Controls.Find(pictureBoxName, True).FirstOrDefault()

                If pictureBox IsNot Nothing Then
                    If File.Exists(defaultImagePath) Then
                        pictureBox.Image = Image.FromFile(defaultImagePath)
                    Else
                        MsgBox($"Default image file not found: {defaultImagePath}")
                    End If
                Else
                    MsgBox($"PictureBox not found: {pictureBoxName}")
                End If

                pictureBoxIndex += 1
            End While
        Else
            MsgBox("Player information not found.")
        End If
    End Sub
    Private Sub LoadPlayer2CardsIntoPictureBoxes(P2position As Integer)

        p2 = ReadPlayerInfoFromJson(P2position)
        Dim noofcards = p2.Cards.Count

        If p2 IsNot Nothing Then

            Dim pictureBoxIndex As Integer = 1
            For Each card In p2.Cards
                If pictureBoxIndex <= noofcards Then
                    Dim pictureBoxName As String = $"Player2Card{pictureBoxIndex}"
                    Dim pictureBox As PictureBox = Me.Controls.Find(pictureBoxName, True).FirstOrDefault()

                    If pictureBox IsNot Nothing Then
                        Dim imagePath As String = $"D:\_Programs\_Visual_Studio_Workspace\Game\Resources\Deck_of_Cards\Horizontal\{card}_Hrtl.png"

                        If File.Exists(imagePath) Then
                            pictureBox.Image = Image.FromFile(imagePath)
                        Else
                            MsgBox($"Image file not found: {imagePath}")
                        End If
                    Else
                        MsgBox($"PictureBox not found: {pictureBoxName}")
                    End If

                    pictureBoxIndex += 1
                Else
                    Exit For
                End If
            Next

            Dim defaultImagePath As String = "D:\_Programs\_Visual_Studio_Workspace\Game\Resources\Horizontal_Card.jpg"

            While pictureBoxIndex <= 5
                Dim pictureBoxName As String = $"Player2Card{pictureBoxIndex}"
                Dim pictureBox As PictureBox = Me.Controls.Find(pictureBoxName, True).FirstOrDefault()

                If pictureBox IsNot Nothing Then
                    If File.Exists(defaultImagePath) Then
                        pictureBox.Image = Image.FromFile(defaultImagePath)
                    Else
                        MsgBox($"Default image file not found: {defaultImagePath}")
                    End If
                Else
                    MsgBox($"PictureBox not found: {pictureBoxName}")
                End If

                pictureBoxIndex += 1
            End While
        Else
            MsgBox("Player information not found.")
        End If
    End Sub
    Private Sub LoadPlayer3CardsIntoPictureBoxes(P3position As Integer)

        p3 = ReadPlayerInfoFromJson(P3position)
        Dim noofcards = p3.Cards.Count
        If p3 IsNot Nothing Then

            Dim pictureBoxIndex As Integer = 1
            For Each card In p3.Cards
                If pictureBoxIndex <= noofcards Then
                    Dim pictureBoxName As String = $"Player3Card{pictureBoxIndex}"
                    Dim pictureBox As PictureBox = Me.Controls.Find(pictureBoxName, True).FirstOrDefault()

                    If pictureBox IsNot Nothing Then
                        Dim imagePath As String = $"D:\_Programs\_Visual_Studio_Workspace\Game\Resources\Deck_of_Cards\Vertical\{card}_Vrtl.png"

                        If File.Exists(imagePath) Then
                            pictureBox.Image = Image.FromFile(imagePath)
                        Else
                            MsgBox($"Image file not found: {imagePath}")
                        End If
                    Else
                        MsgBox($"PictureBox not found: {pictureBoxName}")
                    End If

                    pictureBoxIndex += 1
                Else
                    Exit For
                End If
            Next

            Dim defaultImagePath As String = "D:\_Programs\_Visual_Studio_Workspace\Game\Resources\Vertical_Card.jpg"

            While pictureBoxIndex <= 5
                Dim pictureBoxName As String = $"Player3Card{pictureBoxIndex}"
                Dim pictureBox As PictureBox = Me.Controls.Find(pictureBoxName, True).FirstOrDefault()

                If pictureBox IsNot Nothing Then
                    If File.Exists(defaultImagePath) Then
                        pictureBox.Image = Image.FromFile(defaultImagePath)
                    Else
                        MsgBox($"Default image file not found: {defaultImagePath}")
                    End If
                Else
                    MsgBox($"PictureBox not found: {pictureBoxName}")
                End If

                pictureBoxIndex += 1
            End While
        Else
            MsgBox("Player information not found.")
        End If
    End Sub
    Private Sub LoadPlayer4CardsIntoPictureBoxes(P4position As Integer)
        p4 = ReadPlayerInfoFromJson(P4position)
        Dim noofcards = p4.Cards.Count
        If p4 IsNot Nothing Then

            Dim pictureBoxIndex As Integer = 1
            For Each card In p4.Cards
                If pictureBoxIndex <= noofcards Then
                    Dim pictureBoxName As String = $"Player4Card{pictureBoxIndex}"
                    Dim pictureBox As PictureBox = Me.Controls.Find(pictureBoxName, True).FirstOrDefault()

                    If pictureBox IsNot Nothing Then
                        Dim imagePath As String = $"D:\_Programs\_Visual_Studio_Workspace\Game\Resources\Deck_of_Cards\Horizontal\{card}_Hrtl.png" ' Adjust file extension as per your setup

                        If File.Exists(imagePath) Then
                            pictureBox.Image = Image.FromFile(imagePath)
                        Else
                            MsgBox($"Image file not found: {imagePath}")
                        End If
                    Else
                        MsgBox($"PictureBox not found: {pictureBoxName}")
                    End If

                    pictureBoxIndex += 1
                Else
                    Exit For
                End If
            Next

            Dim defaultImagePath As String = "D:\_Programs\_Visual_Studio_Workspace\Game\Resources\Horizontal_Card.jpg"

            While pictureBoxIndex <= 5
                Dim pictureBoxName As String = $"Player4Card{pictureBoxIndex}"
                Dim pictureBox As PictureBox = Me.Controls.Find(pictureBoxName, True).FirstOrDefault()

                If pictureBox IsNot Nothing Then
                    If File.Exists(defaultImagePath) Then
                        pictureBox.Image = Image.FromFile(defaultImagePath)
                    Else
                        MsgBox($"Default image file not found: {defaultImagePath}")
                    End If
                Else
                    MsgBox($"PictureBox not found: {pictureBoxName}")
                End If

                pictureBoxIndex += 1
            End While
        Else
            MsgBox("Player information not found.")
        End If
    End Sub

    Sub RemoveCardFromFile(filePath As String, cardToRemove As String)
        If filePath = players_hands_JSON_FilePath Then
            Dim currentPlayer As PlayerInfo = ReadPlayerInfoFromJson(1)
            Dim currentPlayerCards As New List(Of String)(currentPlayer.Cards)

            If currentPlayerCards.Contains(cardToRemove) Then
                currentPlayerCards.Remove(cardToRemove)
            End If

            ' Update the player's cards
            currentPlayer.Cards = currentPlayerCards

            ' Read the entire file to update the specific player
            Dim allPlayers As List(Of PlayerInfo) = ReadAllPlayersFromJson(filePath)
            Dim playerIndex As Integer = allPlayers.FindIndex(Function(p) p.Position = currentPlayer.Position)
            If playerIndex >= 0 Then
                allPlayers(playerIndex) = currentPlayer
            End If

            ' Serialize updated list back to JSON
            Dim updatedJson As String = JsonConvert.SerializeObject(allPlayers, Formatting.Indented)
            File.WriteAllText(filePath, updatedJson)
        Else
            Dim json As String = File.ReadAllText(filePath)
            Dim cards As List(Of String) = JsonConvert.DeserializeObject(Of List(Of String))(json)

            cards.Remove(cardToRemove)

            Dim updatedJson As String = JsonConvert.SerializeObject(cards, Formatting.Indented)
            File.WriteAllText(filePath, updatedJson)
        End If
    End Sub

    ' Method to read player info from JSON file based on position
    Public Function ReadPlayerInfoFromJson(Pno As Integer) As PlayerInfo
        Dim players As List(Of PlayerInfo) = ReadAllPlayersFromJson(players_hands_JSON_FilePath)
        Return players.Find(Function(p) p.Position = Pno)
    End Function

    ' Method to read all players from JSON file
    Private Function ReadAllPlayersFromJson(filePath As String) As List(Of PlayerInfo)
        Try
            Dim json As String = File.ReadAllText(filePath)
            Return JsonConvert.DeserializeObject(Of List(Of PlayerInfo))(json)
        Catch ex As Exception
            MsgBox($"Error reading JSON file: {ex.Message}")
            Return New List(Of PlayerInfo)
        End Try
    End Function

    Private Async Sub Timer1_Tick(sender As Object, e As EventArgs) Handles Timer1.Tick
        ' Incrementally rotate the image
        angle += 13
        If angle >= 180 Then
            angle = 0 ' Reset angle after one full rotation
            Timer1.Stop()
            Turn.Invalidate()
            ' Hold for 0.5 seconds
            Await Task.Delay(500)
            Turn.Hide()
            CenterDeck.Show()
            Draw.Enabled = True
        Else
            ' Redraw the PictureBox with the rotated image
            Turn.Invalidate()
        End If
    End Sub

    Private Sub Turn_rotate()
        Try
            ' Load your image into the PictureBox
            Turn.Image = My.Resources.Turn_Rotate
            Turn.SizeMode = PictureBoxSizeMode.StretchImage
        Catch ex As Exception
            MessageBox.Show("Image not found: " & ex.Message)
        End Try

        ' Start the rotation animation
        Turn.Show()

        Timer1.Start()
    End Sub

    Private Sub CardDrew_Paint(sender As Object, e As PaintEventArgs) Handles Turn.Paint
        If Turn.Image IsNot Nothing Then
            Dim g As Graphics = e.Graphics
            g.Clear(Turn.BackColor)
            g.InterpolationMode = InterpolationMode.HighQualityBicubic

            ' Calculate the aspect ratio of the original image
            Dim aspectRatio As Single = Turn.Image.Width / Turn.Image.Height

            ' Calculate the dimensions to fit the PictureBox
            Dim drawWidth As Integer = Turn.Width
            Dim drawHeight As Integer = Turn.Height

            ' Adjust width or height to maintain aspect ratio
            If drawWidth / aspectRatio > drawHeight Then
                drawWidth = CInt(drawHeight * aspectRatio)
            Else
                drawHeight = CInt(drawWidth / aspectRatio)
            End If

            ' Calculate position to center the image
            Dim drawX As Integer = (Turn.Width - drawWidth) \ 2
            Dim drawY As Integer = (Turn.Height - drawHeight) \ 2

            ' Set the rotation point to the center of the PictureBox
            g.TranslateTransform(Turn.Width / 2, Turn.Height / 2)
            g.RotateTransform(-angle) '     - for anti-clockwise

            ' Draw the scaled and rotated image
            g.DrawImage(Turn.Image, New Rectangle(-drawWidth \ 2, -drawHeight \ 2, drawWidth, drawHeight))

            ' Reset transformations
            g.ResetTransform()
        End If
    End Sub

    Private Sub Player1_Paint(sender As Object, e As PaintEventArgs) Handles Player1.Paint
        If p1 IsNot Nothing Then
            Dim g As Graphics = e.Graphics

            Dim text As String = p1.Name
            Dim font As Font = Player1.Font
            Dim brush As Brush = New SolidBrush(Player1.ForeColor)

            Try
                ' Measure the text size
                Dim stringSize As SizeF = g.MeasureString(text, font)
                ' Calculate the center point for vertical and horizontal alignment
                Dim centerPoint As PointF = New PointF(Player1.Width / 2 - stringSize.Width / 2, Player1.Height / 2 - stringSize.Height / 2)

                g.DrawString(text, font, brush, centerPoint)
            Finally
                brush.Dispose() ' Dispose of the brush to free resources
            End Try
        End If
    End Sub
    Private Sub Player2_Paint(sender As Object, e As PaintEventArgs) Handles Player2.Paint
        If p2 IsNot Nothing Then
            Dim g As Graphics = e.Graphics

            Dim text As String = p2.Name
            Dim font As Font = Player2.Font
            Dim brush As Brush = New SolidBrush(Player2.ForeColor)
            Try
                ' Measure the text size
                Dim stringSize As SizeF = g.MeasureString(text, font)

                ' Calculate the center point for vertical and horizontal alignment
                Dim centerPoint As PointF = New PointF(Player2.Width / 2 - stringSize.Width / 2, Player2.Height / 2 - stringSize.Height / 2)

                ' Draw the text vertically if p2 exists
                g.TranslateTransform(centerPoint.X + stringSize.Width / 2, centerPoint.Y + stringSize.Height / 2)
                g.RotateTransform(90) ' Rotate clockwise 90 degrees
                g.DrawString(text, font, brush, -stringSize.Width / 2, -stringSize.Height / 2)
                g.ResetTransform()
            Finally
                brush.Dispose() ' Dispose of the brush to free resources
            End Try
        End If
    End Sub
    Private Sub Player3_Paint(sender As Object, e As PaintEventArgs) Handles Player3.Paint
        If p3 IsNot Nothing Then
            Dim g As Graphics = e.Graphics

            Dim text As String = p3.Name
            Dim font As Font = Player3.Font
            Dim brush As Brush = New SolidBrush(Player1.ForeColor)
            Try
                ' For Player 3, flip upside down
                Dim stringSize As SizeF = g.MeasureString(text, font)
                Dim centerPoint As PointF = New PointF(Player1.Width / 2 - stringSize.Width / 2, Player1.Height / 2 - stringSize.Height / 2)

                g.TranslateTransform(centerPoint.X + stringSize.Width / 2, centerPoint.Y + stringSize.Height / 2)
                g.RotateTransform(180) ' Rotate 180 degrees
                g.DrawString(text, font, brush, -stringSize.Width / 2, -stringSize.Height / 2)
                g.ResetTransform()
            Finally
                brush.Dispose()
            End Try
        End If
    End Sub
    Private Sub Player4_Paint(sender As Object, e As PaintEventArgs) Handles Player4.Paint
        If p4 IsNot Nothing Then
            Dim g As Graphics = e.Graphics

            Dim text As String = p4.Name
            Dim font As Font = Player4.Font
            Dim brush As Brush = New SolidBrush(Player4.ForeColor)
            Try
                ' Measure the text size
                Dim stringSize As SizeF = g.MeasureString(text, font)

                ' Calculate the center point for vertical and horizontal alignment
                Dim centerPoint As PointF = New PointF(Player4.Width / 2 - stringSize.Width / 2, Player4.Height / 2 - stringSize.Height / 2)

                ' Draw the text vertically if p4 exists
                g.TranslateTransform(centerPoint.X + stringSize.Width / 2, centerPoint.Y + stringSize.Height / 2)
                g.RotateTransform(-90) ' Rotate counter-clockwise 90 degrees
                g.DrawString(text, font, brush, -stringSize.Width / 2, -stringSize.Height / 2)
                g.ResetTransform()
            Finally
                brush.Dispose()
            End Try
        End If
    End Sub

    Private Function IsJsonEmpty(JsonFile As String) As Boolean
        Dim json As String = File.ReadAllText(JsonFile).Trim()
        If String.IsNullOrEmpty(json) OrElse json = "{}" OrElse json = "[]" Then
            Return True
        Else
            Return False
        End If
    End Function

    Private Sub GameFinished(Winner As String)
        Entry.UpdateScore(Winner)
        MsgBox("Player " & Winner & " is the Winner. Big W !!!")
        If MessageBox.Show("Do you want to Play Again?", "Play Again or Exit", MessageBoxButtons.YesNo, MessageBoxIcon.Question) = DialogResult.Yes Then
            Initialize_Game()

        Else
            Me.Close()
        End If
    End Sub
End Class

Public Class PlayerInfo
    Public Property Position As Integer
    Public Property Name As String
    Public Property Cards As List(Of String)
End Class