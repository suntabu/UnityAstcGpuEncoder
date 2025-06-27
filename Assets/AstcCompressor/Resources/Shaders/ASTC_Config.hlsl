// ASTC_Config.hlsl

#pragma once

// 支持多种 Block Size 的宏定义
#if defined(BLOCK_SIZE_4x4)
	#define DIM 4
	#define BLOCK_SIZE 16
	#define BLOCK_6X6 0
#elif defined(BLOCK_SIZE_5x5)
	#define DIM 5
	#define BLOCK_SIZE 25
	#define BLOCK_6X6 0
#elif defined(BLOCK_SIZE_6x6)
	#define DIM 6
	#define BLOCK_SIZE 36
	#define BLOCK_6X6 1
#elif defined(BLOCK_SIZE_8x8)
	#define DIM 8
	#define BLOCK_SIZE 64
	#define BLOCK_6X6 0
#elif defined(BLOCK_SIZE_10x10)
	#define DIM 10
	#define BLOCK_SIZE 100
	#define BLOCK_6X6 0
#elif defined(BLOCK_SIZE_12x12)
	#define DIM 12
	#define BLOCK_SIZE 144
	#define BLOCK_6X6 0
#else
	#define DIM 4 // 默认值
	#define BLOCK_SIZE 16
	#define BLOCK_6X6 0
#endif
