﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace Tetris
{
    /// <summary>
    /// The game board
    /// </summary>
    class Board
    {
        /// <summary>
        /// A block spawner
        /// </summary>
        BlockSpawner blockSpawner;

        /// <summary>
        /// The number of rows that have been destroyed
        /// </summary>
        public int rowsDestroyed = 0;

        /// <summary>
        /// The score
        /// </summary>
        public int score = 0;

        /// <summary>
        /// The board that is being played on.
        /// board[row * numColumns + col]. (0, 0) is the upper left.
        /// </summary>
        public double[] board;
        public const double BLOCK_SQUARES = 1;
        public const double EMPTY_SPACE = 0;
        public const double LOCKED_SQUARES = -1;

        /// <summary>
        /// Separate color information so that board can just be passed into a the neural network without much preprocessing
        /// </summary>
        public Color[,] lockedBlocksColors;

        /// <summary>
        /// The block that is currently being played
        /// </summary>
        public Block currentBlock;

        /// <summary>
        /// The number of visible columns on the board
        /// </summary>
        public int numColumns;

        /// <summary>
        /// The number of rows on the board
        /// </summary>
        public int numRows;

        /// <summary>
        /// Whether the player lost the game in the last tick.
        /// </summary>
        public bool hasLost = false;

        /// <summary>
        /// The game board for a game of Tetris
        /// </summary>
        /// <param name="noOfRows">The number of visible rows on the board</param>
        /// <param name="noOfColumns">The number of columns on the board</param>
        public Board(int noOfRows, int noOfColumns, Random rand)
        {
            numColumns = noOfColumns;
            numRows = noOfRows;

            board = new double[numRows * numColumns + 1]; //+1 for bias
            lockedBlocksColors = new Color[numRows, numColumns];
            blockSpawner = new BlockSpawner(rand);

            // initialise the board by setting each cell as the default color
            for(int i = 0; i < board.Length - 1; i++)
            {
                board[i] = EMPTY_SPACE;
            }
            board[board.Length - 1] = 1;

            SpawnBlock();
        }

        public double this[int row, int col]
        {
            get
            {
                if (row < 0 || col < 0 || row >= numRows || col >= numColumns)
                    throw new Exception();
                return board[row * numColumns + col];
            }
            set
            {
                if (row < 0 || col < 0 || row >= numRows || col >= numColumns)
                    throw new Exception();
                board[row * numColumns + col] = value;
            }
        }

        /// <summary>
        /// Ticks the board forward one move
        /// </summary>
        public void Tick()
        {
            if (!CanLowerBlock())
            {
                bool visible = LockBlock();
                if (!visible)
                {
                    hasLost = true;
                    return;
                }
                DestroyFullRows();
                SpawnBlock();
                if(!CanBeHere(currentBlock))
                {
                    hasLost = true;
                }
            }
            else
            {
                TryLowerBlock();
            }
            
        }

        /// <summary>
        /// Creates a new block to play with
        /// </summary>
        private void SpawnBlock()
        {
            // spawn a new block
            currentBlock = blockSpawner.Next();
            currentBlock.topLeft = new Coordinate(-2, (numColumns - currentBlock.boundingSquareSize) / 2);
        }

        /// <summary>
        /// Locks the current block into position on the board
        /// </summary>
        /// <returns>Whether any squares were locked (i.e. whether or not the block is visible)</returns>
        private bool LockBlock()
        {
            bool locked = false;
            // loop through each of the squares within the current block
            foreach(Coordinate squareCoord in currentBlock.squareCoords)
            {
                Coordinate coord = currentBlock.ToBoardCoordinates(squareCoord);
                // lock it into position on the board
                if (coord.row >= 0)
                {
                    lockedBlocksColors[coord.row, coord.col] = currentBlock.color;
                    this[coord.row, coord.col] = LOCKED_SQUARES;
                    locked = true;
                }
            }
            return locked;
        }

        /// <summary>
        /// Checks each of the rows and removes it if it's full, starting at the top and moving down.
        /// </summary>
        private void DestroyFullRows()
        {
            int rowsDestroyedThisTime = 0;

            for (int row = 0; row < numRows; row++)
            {
                if (RowIsFull(row))
                {
                    DestroyRow(row);
                    rowsDestroyedThisTime++;
                }
            }

            // give bonus points for clearing multiple rows at a time
            score += rowsDestroyedThisTime * rowsDestroyedThisTime;
        }

        /// <summary>
        /// Checks to see whether the specified row is full and should be removed
        /// </summary>
        /// <returns>Whether the specified row is full</returns>
        private bool RowIsFull(int row)
        {
            for (int col = 0; col < numColumns; col++)
                if (this[row, col] == EMPTY_SPACE)
                    return false;

            return true;
        }

        /// <summary>
        /// Removes a row from the game board and drops the remaining squares down from above
        /// </summary>
        private void DestroyRow(int rowToRemove)
        {
            // start on the specified row and move up
            for (int row = rowToRemove; row > 0; row--)
            {
                // passing through each column
                for (int col = 0; col < numColumns; col++)
                {
                    // and overwriting the current position with the one above
                    this[row, col] = this[row - 1, col];
                    lockedBlocksColors[row, col] = lockedBlocksColors[row - 1, col];
                }
            }

            rowsDestroyed++;
        }

        #region Block Movement
        /// <summary>
        /// Rotates the block clockwise if possible
        /// </summary>
        public void TryRotateBlock()
        {
            Block rotated = currentBlock.RotatedClockwise();
            if (CanBeHere(rotated))
            {
                currentBlock = rotated;
            }
        }

        public bool CanRotateBlock()
        {
            return CanBeHere(currentBlock.RotatedClockwise());
        }

        /// <summary>
        /// Lowers the current block down one row if possible
        /// </summary>
        public void TryLowerBlock()
        {
            currentBlock.topLeft.row++;

            if (!CanBeHere(currentBlock))
                currentBlock.topLeft.row--;
        }

        public bool CanLowerBlock()
        {
            currentBlock.topLeft.row++;
            bool worked = CanBeHere(currentBlock);
            currentBlock.topLeft.row--;
            return worked;
        }

        /// <summary>
        /// Moves the block left one column if possible
        /// </summary>
        public void TryMoveBlockLeft()
        {
            currentBlock.topLeft.col--;

            if (!CanBeHere(currentBlock))
                currentBlock.topLeft.col++;
        }

        /// <summary>
        /// Moves the block right one column if possible
        /// </summary>
        public void TryMoveBlockRight()
        {
            currentBlock.topLeft.col++;

            if (!CanBeHere(currentBlock))
                currentBlock.topLeft.col--;
        }
        #endregion

        /// <summary>
        /// Returns whether the block is in a valid position
        /// </summary>
        public bool CanBeHere(Block block)
        {
            //Loop through each of the squares within the current block
            foreach (Coordinate squareCoord in block.squareCoords)
            {
                Coordinate coord = block.ToBoardCoordinates(squareCoord);

                if (coord.col >= numColumns || coord.col < 0)
                    return false;

                //Blocks can be above the board
                if (coord.row < 0) continue;

                if (coord.row >= numRows || this[coord.row, coord.col] != EMPTY_SPACE)
                    return false;
            }

            return true;
        }
    }
}
