#pragma once

class GlobalVariables
{
public:
    static GlobalVariables& getInstance()
    {
        static GlobalVariables instance;
        return instance;
    }

    void setHeadPositionAndDirection(
        Windows::Foundation::Numerics::float3 headPosition,
        Windows::Foundation::Numerics::float3 headDirection);

    bool isMoving();

private:
    GlobalVariables() : isFirst(true) {}

    Windows::Foundation::Numerics::float3 lastHeadPosition;
    Windows::Foundation::Numerics::float3 lastHeadDirection;

    Windows::Foundation::Numerics::float3 headPosition;
    Windows::Foundation::Numerics::float3 headDirection;

    bool isFirst;
};
