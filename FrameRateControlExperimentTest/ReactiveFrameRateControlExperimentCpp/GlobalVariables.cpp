#include "pch.h"
#include "GlobalVariables.h"

using namespace Windows::Foundation::Numerics;

void GlobalVariables::setHeadPositionAndDirection(
    Windows::Foundation::Numerics::float3 headPosition,
    Windows::Foundation::Numerics::float3 headDirection)
{
    if ( isFirst )
    {
        this->lastHeadPosition = headPosition;
        this->lastHeadDirection = headDirection;

        isFirst = false;
    }
    else
    {
        this->lastHeadPosition = this->headPosition;
        this->lastHeadDirection = this->headDirection;
    }

    this->headPosition = headPosition;
    this->headDirection = headDirection;
}

bool GlobalVariables::isMoving()
{
    using namespace ::DirectX;

    const auto deltaPosition = length_squared(lastHeadPosition - headPosition);

    OutputDebugStringA(std::to_string(length_squared(lastHeadDirection - headDirection)).c_str());

    return length_squared(lastHeadDirection - headDirection) > 0.000001;
}