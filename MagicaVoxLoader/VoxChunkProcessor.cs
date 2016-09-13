﻿using System.Collections.Generic;
using System.Linq;
using Bawx;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content.Pipeline;

namespace MagicaVoxLoader
{
    [ContentProcessor(DisplayName = "Vox Processor - Chunks")]
    public sealed class VoxChunkProcessor : ContentProcessor<VoxContent, ChunkContentCollection>
    {
        private ContentProcessorContext _context;

        public override ChunkContentCollection Process(VoxContent input, ContentProcessorContext context)
        {
            _context = context;

            var voxels = input.Voxels;

            // load voxels into a grid so we can easily check neighbors
            var grid = BuildGrid(input);

            var actives = new List<BlockData>();
            var inactives = new List<BlockData>();
            for (var i = 0; i < input.Voxels.Length; i++)
            {
                var v = voxels[i];
                // if the voxel is outside the grid or empty, skip it
                if (v.X >= grid.Length || v.Y >= grid[0].Length || v.Z >= grid[0][0].Length || v.IsEmpty)
                    continue;

                // We reduce color index by one so the index matches with our palette array
                var data = new BlockData(voxels[i].X, voxels[i].Y, voxels[i].Z, (byte) (voxels[i].ColorIndex - 1));

                if (IsActive(data.X, data.Y, data.Z, grid))
                    actives.Add(data);
                else
                    inactives.Add(data);
            }

            var chunk = new ChunkContent(Vector3.Zero, input.SizeX, input.SizeY, input.SizeZ, actives.Concat(inactives).ToArray(), input.Palette, actives.Count);
            var chunks = new ChunkContentCollection();
            chunks.Add(chunk);

            return chunks;
        }

        private byte[][][] BuildGrid(VoxContent input)
        {
            _context.Logger.LogMessage($"Building Grid: {input.SizeX}, {input.SizeY}, {input.SizeZ}");

            var grid = new byte[input.SizeX][][];
            for (var x = 0; x < input.SizeX; x++)
            {
                grid[x] = new byte[input.SizeY][];

                for (var y = 0; y < input.SizeY; y++)
                {
                    grid[x][y] = new byte[input.SizeZ];
                }
            }

            foreach (var voxel in input.Voxels)
            {
                if (voxel.X < input.SizeX && voxel.Y < input.SizeY && voxel.Z < input.SizeZ)
                    grid[voxel.X][voxel.Y][voxel.Z] = voxel.ColorIndex;
            }

            return grid;
        }

        private static bool IsActive(byte x, byte y, byte z, byte[][][] grid)
        {
            if (x >= grid.Length || y >= grid[0].Length || z >= grid[0][0].Length)
                return false;

            // check if the block is at the edge of the chunk
            if (x == 0 || y == 0 || z == 0 ||
                x == grid.Length - 1 || y == grid[0].Length - 1 || z == grid[0][0].Length - 1)
                return true;

            // check neighbors
            if (grid[x][y][z + 1] == 0 || grid[x][y][z - 1] == 0 ||
                grid[x][y + 1][z] == 0 || grid[x][y - 1][z] == 0 ||
                grid[x + 1][y][z] == 0 || grid[x - 1][y][z] == 0)
                return true;

            return false;
        }
    }
}