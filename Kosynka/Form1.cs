using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Kosynka
{
    public partial class Form1 : Form
    {
        public const int wCard = 100, hCard = 140;                  // размер карты
        public const int wOffset = wCard / 4, hOffset = hCard / 8;  // расстояния между картами
        public const int wShift = wCard / 5, hShift = hCard / 5; 
        public const int wField = (wCard + wOffset) * 7 + wOffset, hField = (hCard + hOffset) * 5, wWindow = wField + 17, offset = 25, hWindow = hField + 40 + offset;  // учитываем края

        public int time = 0;
        public int variant = 0;
        bool win = false;
        int accent = -1, accent2 = -1, accentLen = 1;

        int oldex, oldey, x, y;    // координаты перемещаемой фигуры
        int candOldPlace, oldPlace, newPlace;    // каждое место имеет свой код от 0 до 11
        bool dragging = false;

        List<Card>[] stacks;       // ну угадайте что это
        List<Card>[] stacksReady;
        List<Card> rest;

        List<Card> buffer;         // для переноса
        List<Card> offer;          // сверху слева доп карты конкретно в данный момент

        Random rnd = new Random();
        int[] used;                // нужно при растасовке карт
        Card winCard;              // карта на анимацию
        int num;                   // который из стекреди будет выкидывать карту на анимацию?
        int countRemember;         // кол-во переложенных карт на последнем ходе (вместо памятного буфера)
        bool needToClose = false;  // скрыть карту при отмене хода

        public Form1()
        {
            InitializeComponent();
            this.Width = wWindow;
            this.Height = hWindow;

            stacks = new List<Card>[7];
            stacksReady = new List<Card>[4];

            for (int i = 0; i < 7; ++i)
            {
                stacks[i] = new List<Card>();
                stacks[i].Clear();
            }

            for (int i = 0; i < 4; ++i)
            {
                stacksReady[i] = new List<Card>();
                stacksReady[i].Clear();
            }

            rest = new List<Card>();
            buffer = new List<Card>();
            offer = new List<Card>();

            used = new int[52];
            for (int i = 0; i < 52; ++i)
            {
                used[i] = i;
            }
            Shuffle();
            FillStacksAndList();
        }

        public void swap(ref int a, ref int b)
        {
            int tmp = a;
            a = b;
            b = tmp;
        }

        private void Shuffle(){
            for (int i = 0; i < 52; ++i)
            {
                swap(ref used[i], ref used[rnd.Next(0, 52)]);
            }
        }

        private void FillStacksAndList()
        {
            int index = 0;

            for (int i = 0; i < 7; ++i)
            {
                for (int j = 0; j <= i; ++j){
                    bool visible = j == i;
                    stacks[i].Add(new Card(used[index] / 13, used[index] % 13 + 1, visible));
                    ++index;
                }
            }

            while (index < 52)
            {
                rest.Add(new Card(used[index] / 13, used[index] % 13 + 1, true));
                ++index;
            }

            // для тестирования финальной анимации
            /*for (int i = 0; i < 52; i++)
            {
                stacksReady[i/13].Add(new Card(used[i] / 13, used[i] % 13 + 1, true));
            }*/
        }

        private String GetNameOfPic(Card card)
        {
            String s = ""; 
            if (card.opened)
            {
                switch (card.suit)
                {
                    case 0: s += "clubs"; break;
                    case 1: s += "hearts"; break;
                    case 2: s += "spades"; break;
                    case 3: s += "diamonds"; break;
                }
                s += card.number;
            }
            else
            {
                s = "shirt" + Data.numShirt;
            }

            return s;
        }

        public static bool IsIn(int x, int y, int start, int start2, int len, int len2)   // находится в прямоугольнике (с учетом offset)
        {
            return x >= start && x <= start + len && y >= start2 && y <= start2 + len2;
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Pen pen = new Pen(Color.Black);
            Pen yellow_pen = new Pen(Color.Yellow, 5);
            Brush bg_brush = new SolidBrush(Color.Green);
            Brush darkgreen_brush = new SolidBrush(Color.DarkGreen);
            Image image = (Image)Kosynka.Properties.Resources.ResourceManager.GetObject("shirt" + Data.numShirt);
            
            g.DrawImage(image, wOffset, offset + hOffset, wCard, hCard);

            // отображаем стеки
            for (int i = 0; i < 7; i++)
            {
                if (stacks[i].Count == 0)
                {
                    /*g.FillRectangle(darkgreen_brush, wOffset + (wOffset + wCard) * i, offset + hOffset * 2 + hCard, wCard, hCard);
                    g.DrawRectangle(pen, wOffset + (wOffset + wCard) * i, offset + hOffset * 2 + hCard, wCard, hCard);*/
                }
                else
                {
                    for (int j = 0; j < stacks[i].Count; j++)
			        {
                        String s = GetNameOfPic(stacks[i][j]);
                        image = (Image)Kosynka.Properties.Resources.ResourceManager.GetObject(s);
                        g.DrawImage(image, wOffset + (wOffset + wCard) * i, offset + hOffset * 2 + hCard + hShift * j, wCard, hCard);
			        }
                }
            }

            // отображаем пазы
            for (int i = 0; i < 4; i++)
            {
                if (stacksReady[i].Count == 0)
                {
                    /*g.FillRectangle(darkgreen_brush, wOffset + (wOffset + wCard) * (i + 3), offset + hOffset, wCard, hCard);
                    g.DrawRectangle(pen, wOffset + (wOffset + wCard) * (i + 3), offset + hOffset, wCard, hCard);*/
                    image = (Image)Kosynka.Properties.Resources.ResourceManager.GetObject("empty");
                    g.DrawImage(image, wOffset + (wOffset + wCard) * (i + 3), offset + hOffset, wCard, hCard);
                }
                else
                {
                    String s = GetNameOfPic(stacksReady[i][stacksReady[i].Count - 1]);
                    image = (Image)Kosynka.Properties.Resources.ResourceManager.GetObject(s);
                    g.DrawImage(image, wOffset + (wOffset + wCard) * (i + 3), offset + hOffset, wCard, hCard);
                }
            }

            // отображаем допы
            if (variant != 0)
            {
                // показать 3 карты
                for (int i = 0; i < offer.Count; i++)
                {
                    if (offer[i] != null)
                    {
                        String s = GetNameOfPic(offer[i]);
                        image = (Image)Kosynka.Properties.Resources.ResourceManager.GetObject(s);
                        g.DrawImage(image, wOffset + wOffset + wCard + wShift * i, offset + hOffset, wCard, hCard);
                    }
                }
            }
            else if (!win)
            {
                g.FillRectangle(bg_brush, wOffset + wOffset + wCard, offset + hOffset, wCard * 2, hCard);
            }

            if (dragging)
            {
                String s;

                for (int i = 0; i < buffer.Count; i++)
                {
                    if (buffer[i] != null)
                    {
                        s = GetNameOfPic(buffer[i]);
                        image = (Image)Kosynka.Properties.Resources.ResourceManager.GetObject(s);
                        g.DrawImage(image, x, y + hShift * i, wCard, hCard);
                    }
                }
                
            }

            // для случая подсказки
            if (accent > -1)
            {
                if (accent < 7)
                {
                    g.DrawRectangle(yellow_pen, wOffset + (wOffset + wCard) * accent, offset + hOffset * 2 + hCard + hShift * ((stacks[accent].Count > 0 ? stacks[accent].Count : stacks[accent].Count + 1) - 1) - (accentLen - 1) * hShift, wCard, hCard + (accentLen - 1) * hShift);
                }
                else if (accent < 11)
                {
                    g.DrawRectangle(yellow_pen, wOffset + (wOffset + wCard) * (accent - 4), offset + hOffset, wCard, hCard);
                }
                else switch (accent)
                    {
                        case 11: g.DrawRectangle(yellow_pen, wOffset + wOffset + wCard + wShift * (offer.Count - 1), offset + hOffset, wCard, hCard); break;
                        case 12: g.DrawRectangle(yellow_pen, wOffset, offset + hOffset, wCard, hCard); break;
                    }
            }

            if (accent2 > -1)
            {
                if (accent2 < 7)
                {
                    g.DrawRectangle(yellow_pen, wOffset + (wOffset + wCard) * accent2, offset + hOffset * 2 + hCard + hShift * ((stacks[accent2].Count > 0 ? stacks[accent2].Count : stacks[accent2].Count + 1) - 1), wCard, hCard);
                }
                else if (accent2 < 11)
                {
                    g.DrawRectangle(yellow_pen, wOffset + (wOffset + wCard) * (accent2 - 4), offset + hOffset, wCard, hCard);
                }
                else switch (accent2)
                    {
                        case 11: g.DrawRectangle(yellow_pen, wOffset + wOffset + wCard + wShift * (offer.Count - 1), offset + hOffset, wCard, hCard); break;
                        case 12: g.DrawRectangle(yellow_pen, wOffset, offset + hOffset, wCard, hCard); break;
                    }
            }

            // для случая победы
            if (win && winCard != null)
            {
                String s = GetNameOfPic(winCard);
                image = (Image)Kosynka.Properties.Resources.ResourceManager.GetObject(s);
                g.DrawImage(image, x, y, wCard, hCard); 
            }

        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            ++time;
            toolStripStatusLabel1.Text = "Время: " + time;
        }

        public class Card
        {
            public int suit;   // 1 - clubs, 2 - hearts, 3 - spades, 4 - diamonds
            public int number; // 11 - валет, 12 - дама, 13 - король
            public bool opened;

            public Card(int suit, int number, bool opened = false)
            {
                this.suit = suit;
                this.number = number;
                this.opened = opened;
            }
        }

        bool isNull(Card card)
        {
            return card == null;
        }

        void ClearNulls(List<Card> list)
        {
            list.RemoveAll(isNull);
        }

        void AddVariant()
        {
            ++variant;
            int max = (rest.Count / 3 > 0 ? rest.Count / 3 : (rest.Count == 0 ? 0 : 1));
            if (variant > max)
            {
                variant = 0;
            }

            // подчищаем пустоты в rest
            if (variant == 1)
                ClearNulls(rest);

            // формируем карты для предложения
            offer.Clear();
            if (variant != 0)
            {
                if (rest.Count > 2)   // тогда 3 карты
                {
                    offer.Add(rest[(variant - 1) * 3]);
                    offer.Add(rest[(variant - 1) * 3 + 1]);
                    offer.Add(rest[(variant - 1) * 3 + 2]);
                }
                else    // тогда что осталось
                {
                    for (int i = 0; i < rest.Count; i++)
                    {
                        offer.Add(rest[i]);
                    }
                }
            }
        }

        void SubVariant()
        {
            --variant;
            if (variant < 0)
            {
                variant = (rest.Count / 3 > 0 ? rest.Count / 3 : (rest.Count == 0 ? 0 : 1));
            }

            // формируем карты для предложения
            offer.Clear();
            if (variant != 0)
            {
                if (rest.Count > 2)   // тогда 3 карты
                {
                    offer.Add(rest[(variant - 1) * 3]);
                    offer.Add(rest[(variant - 1) * 3 + 1]);
                    offer.Add(rest[(variant - 1) * 3 + 2]);
                }
                else    // тогда что осталось
                {
                    for (int i = 0; i < rest.Count; i++)
                    {
                        offer.Add(rest[i]);
                    }
                }
            }

            ClearNulls(offer);
        }

        bool TryStroke(MouseEventArgs e)  // пытаемся сделать ход 
        {
            // если координаты сейчас на прямоугольнике, проверяем, можно ли так, и если да, то переносим с буфера туда
            // проверяем стеки
            for (int i = 0; i < 7; i++)
            {
                int j = stacks[i].Count - 1;
                
                if (IsIn(e.X, e.Y, wOffset + (wOffset + wCard) * i, offset + hOffset * 2 + hCard + hShift * j, wCard, hCard))
                {
                    if (stacks[i].Count == 0 || condNorm(stacks[i][stacks[i].Count-1], buffer[0])){
                        countRemember = buffer.Count;   // для отмены хода
                        oldPlace = candOldPlace;
                        newPlace = i;
                        ToNewPlace();

                        // проверить открылась ли карта
                        needToClose = oldPlace < 7 && stacks[oldPlace].Count > 0 && stacks[oldPlace][stacks[oldPlace].Count-1].opened == false;

                        // удалить в массиве rest
                        if (oldPlace == 11)
                        {
                            rest[rest.IndexOf(buffer[0])] = null;
                        }

                        отменитьХодToolStripMenuItem.Enabled = true;

                        return true;
                    }
                    else{
                        return false;
                    }
                }
                
            }

            // проверяем пазы
            for (int i = 0; i < 4; i++)
            {
                int j = stacks[i].Count - 1;

                if (IsIn(e.X, e.Y, wOffset + (wOffset + wCard) * (i + 3), offset + hOffset, wCard, hCard))
                {
                    if (buffer.Count == 1 && (stacksReady[i].Count == 0 && buffer[0].number == 1 || stacksReady[i].Count > 0 && condNext(stacksReady[i][stacksReady[i].Count - 1], buffer[0])))
                    {
                        countRemember = 1;   // для отмены хода
                        oldPlace = candOldPlace;
                        newPlace = i + 7;
                        ToNewPlace();

                        // проверить открылась ли карта
                        needToClose = oldPlace < 7 && stacks[oldPlace].Count > 0 && stacks[oldPlace][stacks[oldPlace].Count - 1].opened == false;

                        // удалить в массиве rest
                        if (oldPlace == 11)
                        {
                            rest[rest.IndexOf(buffer[0])] = null;
                        }

                        отменитьХодToolStripMenuItem.Enabled = true;

                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }

            }

            return false;
        }

        void Put(int place)
        {
            if (place == 11)
            {
                offer.Add(buffer[0]);
            }
            else if (place >= 7)
            {
                stacksReady[place - 7].Add(buffer[0]);
            }
            else 
            {
                for (int i = 0; i < buffer.Count; i++)
                {
                    stacks[place].Add(buffer[i]);
                }
            }
        }

        void GetBack()   // возврат из буфера на место
        {
            Put(oldPlace);
        }

        void ToNewPlace()  // кладем на новое место
        {
            Put(newPlace);
        }

        void openStacksIfPossible()
        {
            for (int i = 0; i < 7; i++)
            {
                if (stacks[i].Count > 0)
                {
                    stacks[i][stacks[i].Count - 1].opened = true;
                }
            }
        }

        bool condNorm(Card c1, Card c2)   // условие для буфера, чтоб можно было карты друг на друга в стеке класть
        {
            return c1.number - c2.number == 1 && (c1.suit + c2.suit) % 2 == 1;
        }

        bool condNext(Card c1, Card c2)   // условие для пазов, чтоб после червового туза можно было класть только червовую двойку
        {
            return c2.number - c1.number == 1 && c1.suit == c2.suit;
        }

        bool normBuffer()
        {
            for (int i = 0; i < buffer.Count - 1; i++)
            {
                if (!condNorm(buffer[i], buffer[i + 1]))
                {
                    return false;
                }
            }
            return true;
        }

        bool Win()    // критерий победы
        {
            for (int i = 0; i < 4; i++)
            {
                if (stacksReady[i].Count != 13)
                {
                    return false;
                }
            }
            return true;
        }

        int func(int x)
        {
            int our_width = wOffset * (4 + num) + wCard * (3 + num);
            int our_height = hField - hOffset;
            double x_ = x * 11.0 / our_width;

            // это какой-то ужас с этой функцией
            //return offset + hOffset + (x - wOffset * (4 + num) - wCard * (3 + num)) * (x - wOffset * (4 + num) - wCard * (3 + num))/100;
            //return x_ > 0 ? hField - (int)(Math.Abs(x_ * Math.Sin(x_) / 11.0 * our_height)) - hCard + 10 : hField - hCard + 10;
            return x_ > 0 ? hField - (int)(Math.Abs(x_ * Math.Sin(x_) / 11.0 * our_height) / 1.25) - hCard + 10 : hField - hCard + 10;
        }

        Card GetCardForWin()
        {
            num = 0;

            for (int i = 0; i < 3; i++)
            {
                if (stacksReady[i].Count < stacksReady[i + 1].Count)
                {
                    num = i + 1;
                    break;
                }
            }

            Card card = stacksReady[num][stacksReady[num].Count - 1];
            stacksReady[num].RemoveAt(stacksReady[num].Count - 1);

            x = wOffset + (wOffset + wCard) * (num + 3);
            y = func(x);

            return card;
        }

        bool StacksEmpty()
        {
            return stacksReady[0].Count == 0 && stacksReady[1].Count == 0 && stacksReady[2].Count == 0 && stacksReady[3].Count == 0;
        }

        private void Form1_MouseDown(object sender, MouseEventArgs e)
        {
            if (!win)
            {
                accent = -1;
                accent2 = -1;

                if (e.Clicks == 2)
                {
                    // проверяем допы
                    if (IsIn(e.X, e.Y, wOffset + wOffset + wCard + wShift * (offer.Count - 1), offset + hOffset, wCard, hCard))
                    {
                        for (int i = 0; i < 4; i++)
                        {
                            if (stacksReady[i].Count == 0 && offer[offer.Count - 1].number == 1 || stacksReady[i].Count > 0 && condNext(stacksReady[i][stacksReady[i].Count - 1], offer[offer.Count - 1]))
                            {
                                oldPlace = 11;
                                countRemember = 1;   // для отмены хода
                                buffer.Add(offer[offer.Count - 1]);
                                offer.RemoveAt(offer.Count - 1);

                                // удалить в массиве rest
                                if (oldPlace == 11)
                                {
                                    rest[rest.IndexOf(buffer[0])] = null;
                                }

                                newPlace = i + 7;
                                ToNewPlace();

                                отменитьХодToolStripMenuItem.Enabled = true;

                                return;
                            }
                        }
                    }

                    // проверяем стеки
                    for (int i = 0; i < 7; i++)
                    {
                        int j = stacks[i].Count - 1;

                        if (stacks[i].Count > 0 && IsIn(e.X, e.Y, wOffset + (wOffset + wCard) * i, offset + hOffset * 2 + hCard + hShift * j, wCard, hCard) && stacks[i][j].opened)
                        {
                            for (int k = 0; k < 4; k++)
                            {
                                if (stacksReady[k].Count == 0 && stacks[i][stacks[i].Count - 1].number == 1 || stacksReady[k].Count > 0 && condNext(stacksReady[k][stacksReady[k].Count - 1], stacks[i][stacks[i].Count - 1]))
                                {
                                    oldPlace = i;
                                    countRemember = 1;   // для отмены хода
                                    buffer.Add(stacks[i][stacks[i].Count - 1]);
                                    stacks[i].RemoveAt(stacks[i].Count - 1);
                                    needToClose = stacks[i].Count > 0 && stacks[i][stacks[i].Count - 1].opened == false;

                                    newPlace = k + 7;
                                    ToNewPlace();

                                    отменитьХодToolStripMenuItem.Enabled = true;

                                    /*break;*/return;
                                }
                            }
                        }
                        
                    }

                    Form1_MouseUp(sender, e);
                }

                if (e.Clicks == 1 || IsIn(e.X, e.Y, wOffset, offset + hOffset, wCard, hCard))
                {
                    oldex = e.X;
                    oldey = e.Y;

                    if (IsIn(e.X, e.Y, wOffset, offset + hOffset, wCard, hCard))
                    {
                        oldPlace = 12;
                        newPlace = 12;
                        отменитьХодToolStripMenuItem.Enabled = true;
                        AddVariant();
                        Invalidate();
                    }

                    // проверяем допы
                    if (IsIn(e.X, e.Y, wOffset + wOffset + wCard + wShift * (offer.Count - 1), offset + hOffset, wCard, hCard) && offer.Count > 0)
                    {
                        buffer.Add(offer[offer.Count - 1]);
                        offer.RemoveAt(offer.Count - 1);

                        candOldPlace = 11;
                        dragging = true;
                        x = wOffset + wOffset + wCard + wShift * offer.Count;
                        y = offset + hOffset;
                        return;
                    }

                    // проверяем стеки
                    for (int i = 0; i < 7; i++)
                    {
                        for (int j = stacks[i].Count - 1; j >= 0; j--)
                        {
                            if (IsIn(e.X, e.Y, wOffset + (wOffset + wCard) * i, offset + hOffset * 2 + hCard + hShift * j, wCard, hCard) && stacks[i][j].opened)
                            {
                                int k = j;
                                while (k < stacks[i].Count)
                                {
                                    buffer.Add(stacks[i][k]);
                                    stacks[i].RemoveAt(k);
                                }

                                candOldPlace = i;
                                dragging = normBuffer();
                                if (!normBuffer())
                                {
                                    GetBack();
                                    return;
                                }
                                x = wOffset + (wOffset + wCard) * i;
                                y = offset + hOffset * 2 + hCard + hShift * j;
                                /*break;*/return;
                            }
                        }
                    }

                    // проверяем пазы
                    for (int i = 0; i < 4; i++)
                    {
                        int j = stacksReady[i].Count - 1;

                        if (IsIn(e.X, e.Y, wOffset + (wOffset + wCard) * (i + 3), offset + hOffset, wCard, hCard) && stacksReady[i].Count > 0)
                        {
                            buffer.Add(stacksReady[i][j]);
                            stacksReady[i].RemoveAt(j);

                            candOldPlace = i + 7;
                            dragging = true;

                            x = wOffset + (wOffset + wCard) * (i + 3);
                            y = offset + hOffset;
                            /*break;*/return;
                        }
                    }
                }
                
            }        
        }

        private void Form1_MouseUp(object sender, MouseEventArgs e)
        {
            if (dragging)
            {
                if (!TryStroke(e))
                    Put(candOldPlace);
                    /*GetBack();*/
            }
            dragging = false;
            buffer.Clear();
            openStacksIfPossible();
            
            if (!win && Win())
            {
                x = -4;
                y = func(x);
                timer2.Start();
                win = true;
                timer1.Stop();
                отменитьХодToolStripMenuItem.Enabled = false;
                подсказкаToolStripMenuItem.Enabled = false;
            }

            Invalidate();
        }

        private void Form1_MouseMove(object sender, MouseEventArgs e)
        {
            if (dragging)
            {
                x += e.X - oldex;
                y += e.Y - oldey;
                Invalidate();
                oldex = e.X;
                oldey = e.Y;
            }
        }

        private void новаяИграToolStripMenuItem_Click(object sender, EventArgs e)
        {
            time = 0;
            variant = 0;
            win = false;
            dragging = false;
            accent = -1;
            accent2 = -1;

            for (int i = 0; i < 7; ++i)
            {
                stacks[i].Clear();
            }

            for (int i = 0; i < 4; ++i)
            {
                stacksReady[i].Clear();
            }

            rest.Clear();
            buffer.Clear();
            offer.Clear();

            for (int i = 0; i < 52; ++i)
            {
                used[i] = i;
            }
            Shuffle();
            FillStacksAndList();

            отменитьХодToolStripMenuItem.Enabled = false;
            подсказкаToolStripMenuItem.Enabled = true;
            timer2.Stop();
            timer1.Start();

            Invalidate();
        }

        private void выходToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void сменитьРубашкуToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Form2 Form2 = new Form2();
            Form2.ShowDialog();
            Invalidate();
        }

        private void справкаToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Пасьянс \"Косынка\"\nWindows XP\n\nПрограммист: Гумеров Артур", "Справка");
        }

        private void timer2_Tick(object sender, EventArgs e)   // карты падают волнами
        {
            if (x < -wCard || y > hField)    // след карта летит
            {
                if (StacksEmpty())
                    timer2.Stop();
                else
                    winCard = GetCardForWin();
            }
            else   // меняем координаты закономерно по кривой
            {
                x -= 3 + num;
                y = func(x);
                Invalidate();
            }
        }

        private void отменитьХодToolStripMenuItem_Click(object sender, EventArgs e)
        {
            отменитьХодToolStripMenuItem.Enabled = false;

            // если просто нажали на магазин
            if (oldPlace == 12)
            {
                SubVariant();
            }
            else
            {
                // скрыть открытую карту из стека
                if (oldPlace < 7 && needToClose)
                {
                    stacks[oldPlace][stacks[oldPlace].Count - 1].opened = false;
                }

                // переложить countRemember карт с newPlace на oldPlace

                // I. переложить countRemember карт с newPlace на buffer

                for (int i = 0; i < countRemember; i++)
                {
                    // сначала удаляем из нового места
                    Card card;
                    if (newPlace < 7)
                    {
                        card = stacks[newPlace][stacks[newPlace].Count - 1];
                        stacks[newPlace].RemoveAt(stacks[newPlace].Count - 1);
                    }
                    else
                    {
                        card = stacksReady[newPlace - 7][stacksReady[newPlace - 7].Count - 1];    // где тут выход за границы? 
                        stacksReady[newPlace - 7].RemoveAt(stacksReady[newPlace - 7].Count - 1);
                    }

                    // затем в буфер
                    buffer.Add(card);
                }

                // II. переложить countRemember карт с buffer на oldPlace

                for (int i = 0; i < countRemember; i++)
                {
                    // сначала удаляем из буфера
                    Card card = buffer[buffer.Count - 1];
                    buffer.RemoveAt(buffer.Count - 1);

                    // затем возвращаем в старое
                    if (oldPlace < 11)    // если клали из стека или стекареди
                    {
                        if (oldPlace < 7)
                        {
                            stacks[oldPlace].Add(card);
                        }
                        else
                        {
                            stacksReady[oldPlace - 7].Add(card);
                        }
                    }
                    else    // клали из магазина... ну понятное дело тут карта одна
                    {
                        offer.Add(card);
                        // теперь надо вернуть в rest
                        rest[(variant - 1) * 3 + offer.Count - 1] = card;
                    }
                }
            }

            Invalidate();
        }

        private void подсказкаToolStripMenuItem_Click(object sender, EventArgs e)
        {
            accentLen = 1;

            // смотрим где что подходит
            // сначала пытаемся выложить на стекреди что-нибудь
            for (int i = 0; i < 4; i++)
			{
                if (offer.Count > 0 && offer[offer.Count - 1] != null && (stacksReady[i].Count == 0 && offer[offer.Count - 1].number == 1 || stacksReady[i].Count > 0 && condNext(stacksReady[i][stacksReady[i].Count - 1], offer[offer.Count - 1])))
                {
                    accent = 11;
                    accent2 = i + 7;
                    Invalidate();
                    return;
                }
			}

            for (int j = 0; j < 7; j++)
            {
                for (int i = 0; i < 4; i++)
			    {
                    if (stacks[j].Count > 0 && (stacksReady[i].Count == 0 && stacks[j][stacks[j].Count - 1].number == 1 || stacksReady[i].Count > 0 && condNext(stacksReady[i][stacksReady[i].Count - 1], stacks[j][stacks[j].Count - 1])))
                    {
                        accent = j;
                        accent2 = i + 7;
                        Invalidate();
                        return;
                    }
			    }
            }

            // потом хотим переложить с магазина на стеки, куда подходит?
            for (int i = 0; i < 7; i++)
            {
                if (offer.Count > 0 && offer[offer.Count - 1] != null && (stacks[i].Count == 0 || stacks[i].Count > 0 && condNorm(stacks[i][stacks[i].Count - 1], offer[offer.Count - 1])))
                {
                    accent = 11;
                    accent2 = i;
                    Invalidate();
                    return;
                }
            }

            // потом со стека на стек хотим перекладывать (с j на i)
            for (int j = 6; j >= 0; j--)
                for (int i = 6; i >= 0; i--)
                {
                    int k = 0;
                    while (k < stacks[j].Count && !stacks[j][k].opened) ++k;

                    if (i != j && stacks[j].Count > 0 && (stacks[i].Count == 0 || stacks[i].Count > 0 && condNorm(stacks[i][stacks[i].Count - 1], stacks[j][k])))
                    {
                        accentLen = stacks[j].Count - k;
                        if (stacks[i].Count == 0 && k == 0) continue;
                        accent = j;
                        accent2 = i;
                        Invalidate();
                        return;
                    }
                }

            if (rest.Count > 0)
                accent = 12;
            else MessageBox.Show("Ходов нет", "Сообщение");
            accent2 = -1;
            Invalidate();
        }
    }

}
